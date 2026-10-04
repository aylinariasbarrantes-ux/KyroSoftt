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

        public HomeController(ILogger<HomeController> logger, LaboratorioService laboratorioService)
        {
            _logger = logger;
            _laboratorioService = laboratorioService;
        }

        // La protección de sesión la aplica el filtro [SesionRequerida]
        [SesionRequerida]
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");

            if (!string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase))
            {
                // Solo se muestran laboratorios que pueden utilizarse para una consulta.
                ViewBag.Laboratorios = _laboratorioService.ListarLaboratorios()
                    .Where(l => string.Equals(l.Estado, "Habilitado", StringComparison.OrdinalIgnoreCase))
                    .ToList();
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
