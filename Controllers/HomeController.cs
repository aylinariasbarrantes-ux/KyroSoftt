using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SistemaReservasLaboratorios.Filters;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // La protección de sesión la aplica el filtro [SesionRequerida]
        [SesionRequerida]
        public IActionResult Index()
        {
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
