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

        public ReservaController(ReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        // GET: consulta si un laboratorio está libre en el rango indicado.
        // Devuelve JSON porque aún no existe la vista de reservas.
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
                return BadRequest(DisponibilidadResultado.NoDisponible(
                    $"La hora de fin no es válida. Usa el formato {FormatoHora}."));
            }

            var resultado = _reservaService.ConsultarDisponibilidad(
                laboratorioId, fechaParseada, inicioParseado, finParseado);

            return Ok(resultado);
        }
    }
}
