
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Services
{
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
                .Where(r => r.Estado != EstadoCancelada)
                .Include(r => r.Laboratorio)
                .OrderBy(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .ToList();
        }

        public DisponibilidadResultado ConsultarDisponibilidad(
            int laboratorioId,
            DateOnly fecha,
            TimeOnly horaInicio,
            TimeOnly horaFin)
        {
            // HU5: Validaciones unitarias de fecha y horario.
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var errorHorario = ReglasReserva.ValidarHorario(
                fecha,
                horaInicio,
                horaFin,
                hoy);

            if (errorHorario != null)
            {
                return DisponibilidadResultado.NoDisponible(errorHorario);
            }

            // El laboratorio debe existir.
            var laboratorio = _context.Laboratorios
                .AsNoTracking()
                .FirstOrDefault(l => l.Id == laboratorioId);

            if (laboratorio == null)
            {
                return DisponibilidadResultado.NoDisponible(
                    "El laboratorio no existe.");
            }

            // Un laboratorio fuera de servicio no puede reservarse.
            if (laboratorio.Estado == EstadoFueraDeServicio)
            {
                return DisponibilidadResultado.NoDisponible(
                    "El laboratorio está fuera de servicio.");
            }

            // HU6: Comprobar conflictos con reservas activas.
            // Primero filtramos en la base de datos por laboratorio y fecha.
            // Después utilizamos la regla pura para comparar horarios.
            var reservasExistentes = _context.Reservas
                .AsNoTracking()
                .Where(r => r.LaboratorioId == laboratorioId
                            && r.Fecha == fecha
                            && r.Estado != EstadoCancelada)
                .Select(r => new
                {
                    r.HoraInicio,
                    r.HoraFin
                })
                .ToList();

            var hayTraslape = reservasExistentes.Any(r =>
                ReglasReserva.HayTraslape(
                    r.HoraInicio,
                    r.HoraFin,
                    horaInicio,
                    horaFin));

            if (hayTraslape)
            {
                return DisponibilidadResultado.NoDisponible(
                    "El laboratorio ya tiene una reserva en ese horario.");
            }

            return DisponibilidadResultado.Libre();
        }

        public DisponibilidadResultado RegistrarReserva(
            int laboratorioId,
            DateOnly fecha,
            TimeOnly horaInicio,
            TimeOnly horaFin,
            string responsable)
        {
            if (string.IsNullOrWhiteSpace(responsable))
            {
                return DisponibilidadResultado.NoDisponible(
                    "No se pudo identificar al responsable de la reserva.");
            }

            var disponibilidad = ConsultarDisponibilidad(
                laboratorioId,
                fecha,
                horaInicio,
                horaFin);

            if (!disponibilidad.Disponible)
            {
                return disponibilidad;
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


        public OperacionResultado CancelarReserva(
            int reservaId,
            string usuario,
            bool esAdministrador)
        {
            var reserva = _context.Reservas
                .FirstOrDefault(r => r.Id == reservaId);

            if (reserva == null)
            {
                return OperacionResultado.Error(
                    "La reserva no existe.");
            }

            if (reserva.Estado == EstadoCancelada)
            {
                return OperacionResultado.Error(
                    "La reserva ya estaba cancelada.");
            }

            // HU8: Validar permisos mediante una regla unitaria.
            var tienePermiso = ReglasCancelacion.PuedeCancelar(
                reserva.Responsable,
                usuario,
                esAdministrador);

            if (!tienePermiso)
            {
                return OperacionResultado.Error(
                    "No puede cancelar una reserva de otro usuario.");
            }

            // HU8: Validar expiración mediante una regla unitaria.
            var yaExpirada = ReglasCancelacion.EstaExpirada(
                reserva.Fecha,
                reserva.HoraFin,
                DateTime.Now);

            if (yaExpirada)
            {
                return OperacionResultado.Error(
                    "No se puede cancelar una reserva que ya expiró.");
            }

            reserva.Estado = EstadoCancelada;
            _context.SaveChanges();

            return OperacionResultado.Ok(
                "La reserva fue cancelada correctamente.");
        }


        private IQueryable<Reserva> ConsultaReservasProximas()
        {
            var ahora = DateTime.Now;
            var hoy = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            return _context.Reservas
                .AsNoTracking()
                .Where(r => r.Estado != EstadoCancelada
                            && (r.Fecha > hoy ||
                                (r.Fecha == hoy &&
                                 r.HoraFin > horaActual)));
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
    }
}
