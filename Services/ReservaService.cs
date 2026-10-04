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

        private readonly AppDbContext _context;

        public ReservaService(AppDbContext context)
        {
            _context = context;
        }

        public List<Reserva> ObtenerReservasCalendario()
        {
            return _context.Reservas
                .AsNoTracking()
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
                return DisponibilidadResultado.NoDisponible("El horario ingresado no es válido.");
            }

            // 2. No se permiten consultas al pasado
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            if (fecha < hoy)
            {
                return DisponibilidadResultado.NoDisponible("No se pueden consultar fechas pasadas.");
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

        private static bool EsIntervaloDeTreintaMinutos(TimeOnly hora)
        {
            return hora.Minute == 0 || hora.Minute == 30;
        }
    }
}
