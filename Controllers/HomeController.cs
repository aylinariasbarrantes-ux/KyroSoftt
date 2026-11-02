using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SistemaReservasLaboratorios.Filters;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;

namespace SistemaReservasLaboratorios.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LaboratorioService _laboratorioService;
        private readonly ReservaService _reservaService;

        public HomeController(
            ILogger<HomeController> logger,
            LaboratorioService laboratorioService,
            ReservaService reservaService)
        {
            _logger = logger;
            _laboratorioService = laboratorioService;
            _reservaService = reservaService;
        }

        // La protección de sesión la aplica el filtro [SesionRequerida]
        [SesionRequerida]
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");
            var laboratorios = _laboratorioService.ListarLaboratorios().ToList();

            if (!string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase))
            {
                // Solo se muestran laboratorios que pueden utilizarse para una consulta.
                ViewBag.Laboratorios = laboratorios
                    .Where(l => string.Equals(l.Estado, "Habilitado", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                // Datos reales para el panel del administrador
                var habilitados = laboratorios.Count(l => string.Equals(l.Estado, "Habilitado", StringComparison.OrdinalIgnoreCase));

                ViewBag.TotalLaboratorios = laboratorios.Count;
                ViewBag.LaboratoriosHabilitados = habilitados;
                ViewBag.LaboratoriosFueraDeServicio = laboratorios.Count - habilitados;
                ViewBag.ReservasProximas = _reservaService.ContarReservasProximas();
                ViewBag.ProximasReservas = _reservaService.ObtenerProximasReservas(5);
            }

            return View();
        }

        [SesionRequerida]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}