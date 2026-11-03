using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Services
{
    // Servicio de reservas, separado de LaboratorioService a propósito:
    // aquí vive la lógica de disponibilidad/ocupación, no el CRUD de laboratorios.
    public class ReservaService
    {
        private const string EstadoFueraDeServicio = "Fuera de servicio";
        private const string EstadoCancelada = "Cancelada";
        private const double DuracionMaximaHoras = 4;
        private static readonly TimeOnly HoraApertura = new(8, 0);
        private static readonly TimeOnly HoraCierre = new(22, 0);

        private readonly AppDbContext _context;

        public ReservaService(AppDbContext context)
        {
            _context = context;
        }

        public List<Reserva> ObtenerReservasCalendario()
        {
            return _context.Reservas
                .AsNoTracking()
                // Las canceladas ya no ocupan el horario ni deben aparecer en el calendario.
                .Where(r => r.Estado != EstadoCancelada)
                .Include(r => r.Laboratorio)
                .OrderBy(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .ToList();
        }

        // Indica si un laboratorio puede reservarse en el rango [horaInicio, horaFin) de esa fecha.
        // Devuelve siempre un resultado con el motivo de un eventual rechazo.
        public DisponibilidadResultado ConsultarDisponibilidad(
            int laboratorioId, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
        {
            // 1. El rango horario debe ser coherente
            if (!EsIntervaloDeTreintaMinutos(horaInicio) || !EsIntervaloDeTreintaMinutos(horaFin))
            {
                return DisponibilidadResultado.NoDisponible(
                    "Las horas deben estar en intervalos de 30 minutos (:00 o :30).");
            }

            if (horaInicio >= horaFin)
            {
                return DisponibilidadResultado.NoDisponible("La hora final debe ser posterior a la hora inicial.");
            }

            if (horaInicio < HoraApertura || horaFin > HoraCierre)
            {
                return DisponibilidadResultado.NoDisponible("El horario permitido es de 8:00 a. m. a 10:00 p. m.");
            }

            // Nueva regla (HU-5): duración máxima de 4 horas
            var duracion = horaFin.ToTimeSpan() - horaInicio.ToTimeSpan();
            if (duracion.TotalHours > DuracionMaximaHoras)
            {
                return DisponibilidadResultado.NoDisponible(
                    $"La reserva no puede superar las {DuracionMaximaHoras} horas de duración.");
            }

            // 2. No se permiten consultas al pasado
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            if (fecha < hoy)
            {
                return DisponibilidadResultado.NoDisponible("La fecha seleccionada no puede estar en el pasado.");
            }

            // 3. El laboratorio debe existir
            var laboratorio = _context.Laboratorios
                .AsNoTracking()
                .FirstOrDefault(l => l.Id == laboratorioId);

            if (laboratorio == null)
            {
                return DisponibilidadResultado.NoDisponible("El laboratorio no existe.");
            }

            // 4. Un laboratorio fuera de servicio no se consulta contra sus reservas
            if (laboratorio.Estado == EstadoFueraDeServicio)
            {
                return DisponibilidadResultado.NoDisponible("El laboratorio está fuera de servicio.");
            }

            // 5. Traslape contra las reservas activas del mismo día.
            // Se filtra en la base de datos; las canceladas no bloquean.
            var hayTraslape = _context.Reservas
                .AsNoTracking()
                .Any(r => r.LaboratorioId == laboratorioId
                          && r.Fecha == fecha
                          && r.Estado != EstadoCancelada
                          && horaInicio < r.HoraFin
                          && horaFin > r.HoraInicio);

            if (hayTraslape)
            {
                return DisponibilidadResultado.NoDisponible("El laboratorio ya tiene una reserva en ese horario.");
            }

            // 6. Pasó todas las validaciones
            return DisponibilidadResultado.Libre();
        }

        // Registra la reserva si pasa todas las validaciones de ConsultarDisponibilidad.
        // Devuelve el resultado: si Disponible es true, la reserva ya quedó guardada.
        public DisponibilidadResultado RegistrarReserva(
            int laboratorioId, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin, string responsable)
        {
            if (string.IsNullOrWhiteSpace(responsable))
            {
                return DisponibilidadResultado.NoDisponible("No se pudo identificar al responsable de la reserva.");
            }

            // Reutiliza TODAS las validaciones de horario, fecha, duración, conflicto y estado del laboratorio
            var disponibilidad = ConsultarDisponibilidad(laboratorioId, fecha, horaInicio, horaFin);

            if (!disponibilidad.Disponible)
            {
                return disponibilidad; // trae el motivo exacto del rechazo
            }

            var reserva = new Reserva
            {
                LaboratorioId = laboratorioId,
                Fecha = fecha,
                HoraInicio = horaInicio,
                HoraFin = horaFin,
                Responsable = responsable,
                Estado = "Activa"
            };

            _context.Reservas.Add(reserva);
            _context.SaveChanges();

            return DisponibilidadResultado.Libre();
        }

        // HU-7: reservas del usuario indicado, de la más reciente a la más antigua.
        public List<Reserva> ObtenerMisReservas(string responsable)
        {
            return _context.Reservas
                .AsNoTracking()
                .Include(r => r.Laboratorio)
                .Where(r => r.Responsable == responsable)
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .ToList();
        }

        // HU-8: cancela (cambia el estado, no borra el registro).
        // El dueño puede cancelar la suya; el administrador cualquiera.
        public OperacionResultado CancelarReserva(int reservaId, string usuario, bool esAdministrador)
        {
            var reserva = _context.Reservas.FirstOrDefault(r => r.Id == reservaId);

            if (reserva == null)
            {
                return OperacionResultado.Error("La reserva no existe.");
            }

            if (reserva.Estado == EstadoCancelada)
            {
                return OperacionResultado.Error("La reserva ya estaba cancelada.");
            }

            var esDueno = string.Equals(reserva.Responsable, usuario, StringComparison.OrdinalIgnoreCase);
            if (!esAdministrador && !esDueno)
            {
                return OperacionResultado.Error("No puede cancelar una reserva de otro usuario.");
            }

            reserva.Estado = EstadoCancelada;
            _context.SaveChanges();

            return OperacionResultado.Ok("La reserva fue cancelada correctamente.");
        }

        // Reservas activas que aún no terminaron (para el panel principal del administrador).
        private IQueryable<Reserva> ConsultaReservasProximas()
        {
            var ahora = DateTime.Now;
            var hoy = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            return _context.Reservas
                .AsNoTracking()
                .Where(r => r.Estado != EstadoCancelada
                            && (r.Fecha > hoy || (r.Fecha == hoy && r.HoraFin > horaActual)));
        }

        public List<Reserva> ObtenerProximasReservas(int cantidad)
        {
            return ConsultaReservasProximas()
                .Include(r => r.Laboratorio)
                .OrderBy(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .Take(cantidad)
                .ToList();
        }

        public int ContarReservasProximas()
        {
            return ConsultaReservasProximas().Count();
        }

        private static bool EsIntervaloDeTreintaMinutos(TimeOnly hora)
        {
            return hora.Minute == 0 || hora.Minute == 30;
        }
    }
}