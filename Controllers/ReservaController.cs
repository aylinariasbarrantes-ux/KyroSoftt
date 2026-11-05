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
            var esAdministrador = EsAdministrador();

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
            var error = ReglasFormularioReserva.ParsearHorario(
    laboratorioId, fecha, horaInicio, horaFin,
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
            var error = ReglasFormularioReserva.ParsearHorario(
    laboratorioId, fecha, horaInicio, horaFin,
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

        // HU-7: reservas del usuario autenticado. El usuario se toma de la sesión,
        // nunca de un parámetro, por lo que el administrador también ve solo las suyas.

        [HttpGet]
        public IActionResult MisReservas()
        {
            var usuario = HttpContext.Session.GetString("NombreUsuario");

            if (string.IsNullOrWhiteSpace(usuario))
            {
                return BadRequest(OperacionResultado.Error(
                    "No se pudo identificar al usuario en sesión. Inicie sesión nuevamente."));
            }

            // Se obtiene una sola vez para evaluar todas las reservas.
            var ahora = DateTime.Now;

            var reservas = _reservaService.ObtenerMisReservas(usuario)
                .Select(r => new MiReservaItemViewModel
                {
                    Id = r.Id,
                    LaboratorioId = r.LaboratorioId,

                    Laboratorio = r.Laboratorio?.Nombre
                        ?? $"Laboratorio {r.LaboratorioId}",

                    Fecha = r.Fecha.ToString(FormatoFecha),
                    HoraInicio = r.HoraInicio.ToString(FormatoHora),
                    HoraFin = r.HoraFin.ToString(FormatoHora),

                    // HU7: Estado visible calculado mediante regla unitaria.
                    Estado = ReglasMisReservas.ObtenerEstadoVisible(
                        r.Estado,
                        r.Fecha,
                        r.HoraFin,
                        ahora),

                    // HU7: Determina si se muestra la opción de cancelar.
                    PuedeCancelar = ReglasMisReservas.PuedeMostrarCancelacion(
                        r.Estado,
                        r.Fecha,
                        r.HoraFin,
                        ahora)
                })
                .ToList();

            return View(reservas);
        }


        // HU-8: cancela la reserva indicada. Usuario y rol se resuelven solo desde
        // la sesión, para que nadie pueda cancelar a nombre de otro desde el formulario.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelarReserva(int reservaId)
        {
            if (reservaId <= 0)
            {
                return BadRequest(OperacionResultado.Error("Debe indicar una reserva válida."));
            }

            var usuario = HttpContext.Session.GetString("NombreUsuario");
            if (string.IsNullOrWhiteSpace(usuario))
            {
                // No debería ocurrir: [SesionRequerida] ya exige NombreUsuario.
                return BadRequest(OperacionResultado.Error(
                    "No se pudo identificar al usuario en sesión. Inicie sesión nuevamente."));
            }

            var resultado = _reservaService.CancelarReserva(reservaId, usuario, EsAdministrador());

            // Se devuelve el mensaje exacto del servicio, sin inventar textos nuevos.
            if (!resultado.Exito)
            {
                return BadRequest(resultado);
            }

            return Ok(resultado);
        }

        // Comparación de rol centralizada, usada por Index, MisReservas y CancelarReserva.
        private bool EsAdministrador()
        {
            var rol = HttpContext.Session.GetString("Rol");
            return string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase);
        }

        
    }
}
