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
            // Se exige que el laboratorio venga informado
            if (laboratorioId <= 0)
            {
                return BadRequest(DisponibilidadResultado.NoDisponible("Debe indicar un laboratorio válido."));
            }

            // Formato de fecha: yyyy-MM-dd
            if (!DateOnly.TryParseExact(fecha, FormatoFecha, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fechaParseada))
            {
                return BadRequest(DisponibilidadResultado.NoDisponible(
                    $"La fecha no es válida. Usa el formato {FormatoFecha}."));
            }

            // Formato de hora: HH:mm
            if (!TimeOnly.TryParseExact(horaInicio, FormatoHora, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var inicioParseado))
            {
                return BadRequest(DisponibilidadResultado.NoDisponible(
                    $"La hora de inicio no es válida. Usa el formato {FormatoHora}."));
            }

            if (!TimeOnly.TryParseExact(horaFin, FormatoHora, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var finParseado))
            {
                return BadRequest(DisponibilidadResultado.NoDisponible($"La hora de fin no es válida. Usa el formato {FormatoHora}."));
            }

            if (!EsIntervaloDeTreintaMinutos(inicioParseado) || !EsIntervaloDeTreintaMinutos(finParseado))
            {
                return BadRequest(DisponibilidadResultado.NoDisponible(
                    "Las horas deben seleccionarse en intervalos de 30 minutos (:00 o :30)."));
            }

            if (inicioParseado >= finParseado)
            {
                return BadRequest(DisponibilidadResultado.NoDisponible(
                    $"La hora de fin no es válida. Usa el formato {FormatoHora}."));
            }

            var resultado = _reservaService.ConsultarDisponibilidad(
                laboratorioId, fechaParseada, inicioParseado, finParseado);

            return Ok(resultado);
        }

        private static bool EsIntervaloDeTreintaMinutos(TimeOnly hora)
        {
            return hora.Minute == 0 || hora.Minute == 30;
        }
    }
}
