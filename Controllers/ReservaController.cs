using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SistemaReservasLaboratorios.Filters;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;

namespace SistemaReservasLaboratorios.Controllers
{
    // Controlador de reservas. La disponibilidad se valida en ReservaService,
    // no en LaboratorioController, que queda enfocado solo en laboratorios.
    [SesionRequerida]
    public class ReservaController : Controller
    {
        private const string FormatoFecha = "yyyy-MM-dd";
        private const string FormatoHora = "HH:mm";

        private readonly ReservaService _reservaService;
        private readonly LaboratorioService _laboratorioService;

        public ReservaController(ReservaService reservaService, LaboratorioService laboratorioService)
        {
            _reservaService = reservaService;
            _laboratorioService = laboratorioService;
        }

        // El calendario puede verlo cualquier usuario autenticado.
        // Los usuarios regulares reciben únicamente laboratorio y horario;
        // los datos administrativos no se envían al navegador.
        [HttpGet]
        public IActionResult Index()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var rol = HttpContext.Session.GetString("Rol");
            var esAdministrador = string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase);

            var model = new ReservaCalendarioViewModel
            {
                EsAdministrador = esAdministrador,
                FechaSeleccionada = hoy.ToString(FormatoFecha),
                Laboratorios = _laboratorioService.ListarLaboratorios()
                    .Select(l => new ReservaCalendarioLaboratorioViewModel
                    {
                        Id = l.Id,
                        Nombre = l.Nombre
                    })
                    .ToList(),
                Reservas = _reservaService.ObtenerReservasCalendario()
                    .Select(r => new ReservaCalendarioItemViewModel
                    {
                        Id = r.Id,
                        LaboratorioId = r.LaboratorioId,
                        LaboratorioNombre = r.Laboratorio?.Nombre ?? $"Laboratorio {r.LaboratorioId}",
                        // Para usuarios regulares estos campos se dejan vacíos a propósito,
                        // para que no puedan recuperarlos inspeccionando el HTML o JavaScript.
                        LaboratorioUbicacion = esAdministrador ? (r.Laboratorio?.Ubicacion ?? string.Empty) : string.Empty,
                        Responsable = esAdministrador ? r.Responsable : string.Empty,
                        Fecha = r.Fecha.ToString(FormatoFecha),
                        HoraInicio = r.HoraInicio.ToString(FormatoHora),
                        HoraFin = r.HoraFin.ToString(FormatoHora),
                        Estado = esAdministrador ? r.Estado : string.Empty
                    })
                    .ToList()
            };

            return View(model);
        }

        // Este endpoint sí está disponible para cualquier usuario autenticado,
        // porque el Home del usuario regular lo utiliza para consultar disponibilidad.
        [HttpGet]
        public IActionResult ConsultarDisponibilidad(
            int laboratorioId, string fecha, string horaInicio, string horaFin)
        {
            // Parseo y validación de formato compartidos con RegistrarReserva
            var error = ParsearHorario(laboratorioId, fecha, horaInicio, horaFin,
                out var fechaParseada, out var inicioParseado, out var finParseado);

            if (error != null)
            {
                return BadRequest(error);
            }

            var resultado = _reservaService.ConsultarDisponibilidad(
                laboratorioId, fechaParseada, inicioParseado, finParseado);

            return Ok(resultado);
        }

        // Registra una reserva con los datos enviados por el formulario.
        // El responsable NUNCA llega como parámetro: se toma de la sesión en el
        // servidor, para que un usuario no pueda reservar a nombre de otra persona.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarReserva(
            int laboratorioId, string fecha, string horaInicio, string horaFin)
        {
            // Mismo parseo/validación de formato que ConsultarDisponibilidad
            var error = ParsearHorario(laboratorioId, fecha, horaInicio, horaFin,
                out var fechaParseada, out var inicioParseado, out var finParseado);

            if (error != null)
            {
                return BadRequest(error);
            }

            // El responsable se resuelve server-side desde la sesión.
            var responsable = HttpContext.Session.GetString("NombreUsuario");
            if (string.IsNullOrWhiteSpace(responsable))
            {
                // No debería ocurrir: [SesionRequerida] ya exige NombreUsuario.
                // Aun así, se rechaza antes que guardar una reserva sin responsable.
                return BadRequest(DisponibilidadResultado.NoDisponible(
                    "No se pudo identificar al responsable de la reserva. Inicie sesión nuevamente."));
            }

            var resultado = _reservaService.RegistrarReserva(
                laboratorioId, fechaParseada, inicioParseado, finParseado, responsable);

            // Si Disponible es false se devuelve el motivo exacto que ya trae el servicio.
            if (!resultado.Disponible)
            {
                return BadRequest(resultado);
            }

            // Disponible == true: la reserva ya quedó guardada dentro del servicio.
            return Ok(resultado);
        }

        // Convierte los textos del formulario/query a tipos fuertes con el formato esperado.
        // Devuelve null si todo es válido, o el DisponibilidadResultado con el motivo del rechazo.
        // Lo usan tanto ConsultarDisponibilidad como RegistrarReserva para no duplicar validaciones.
        private static DisponibilidadResultado? ParsearHorario(
            int laboratorioId, string? fecha, string? horaInicio, string? horaFin,
            out DateOnly fechaParseada, out TimeOnly inicioParseado, out TimeOnly finParseado)
        {
            fechaParseada = default;
            inicioParseado = default;
            finParseado = default;

            // Se exige que el laboratorio venga informado
            if (laboratorioId <= 0)
            {
                return DisponibilidadResultado.NoDisponible("Debe indicar un laboratorio válido.");
            }

            // Formato de fecha: yyyy-MM-dd
            if (!DateOnly.TryParseExact(fecha, FormatoFecha, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out fechaParseada))
            {
                return DisponibilidadResultado.NoDisponible(
                    $"La fecha no es válida. Usa el formato {FormatoFecha}.");
            }

            // Formato de hora: HH:mm
            if (!TimeOnly.TryParseExact(horaInicio, FormatoHora, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out inicioParseado))
            {
                return DisponibilidadResultado.NoDisponible(
                    $"La hora de inicio no es válida. Usa el formato {FormatoHora}.");
            }

            if (!TimeOnly.TryParseExact(horaFin, FormatoHora, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out finParseado))
            {
                return DisponibilidadResultado.NoDisponible(
                    $"La hora de fin no es válida. Usa el formato {FormatoHora}.");
            }

            if (!EsIntervaloDeTreintaMinutos(inicioParseado) || !EsIntervaloDeTreintaMinutos(finParseado))
            {
                return DisponibilidadResultado.NoDisponible(
                    "Las horas deben seleccionarse en intervalos de 30 minutos (:00 o :30).");
            }

            if (inicioParseado >= finParseado)
            {
                return DisponibilidadResultado.NoDisponible(
                    "La hora final debe ser posterior a la hora inicial.");
            }

            return null;
        }

        private static bool EsIntervaloDeTreintaMinutos(TimeOnly hora)
        {
            return hora.Minute == 0 || hora.Minute == 30;
        }
    }
}
