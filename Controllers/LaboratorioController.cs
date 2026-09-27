using Microsoft.AspNetCore.Mvc;
using SistemaReservasLaboratorios.Filters;
using SistemaReservasLaboratorios.Services;

namespace SistemaReservasLaboratorios.Controllers
{
    // El filtro a nivel de clase protege todas las acciones de este controlador
    [SesionRequerida]
    public class LaboratorioController : Controller
    {
        private readonly LaboratorioService _laboratorioService;

        public LaboratorioController(LaboratorioService laboratorioService)
        {
            _laboratorioService = laboratorioService;
        }

        // GET: lista los laboratorios registrados
        public IActionResult Index()
        {
            var laboratorios = _laboratorioService.ListarLaboratorios();
            return View(laboratorios);
        }
    }
}
