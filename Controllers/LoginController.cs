using Microsoft.AspNetCore.Mvc;
using SistemaReservasLaboratorios.Services;

namespace SistemaReservasLaboratorios.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthService _authService;

        public LoginController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: muestra el formulario de login
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // POST: procesa el formulario
        [HttpPost]
        public IActionResult Index(string nombreUsuario, string password)
        {
            // Validar que los campos no estén vacíos
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Debe ingresar usuario y contraseña.";
                return View();
            }

            var usuario = _authService.ValidarCredenciales(nombreUsuario, password);

            if (usuario == null)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View();
            }

            // Guardar al usuario en sesión para identificarlo durante el uso de la app
            HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);
            HttpContext.Session.SetString("Rol", usuario.Rol);

            // Redirige según el rol (por ahora al Home, luego pueden diferenciarlo)
            return RedirectToAction("Index", "Home");
        }
    }
}