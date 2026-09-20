using Microsoft.AspNetCore.Mvc;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;

namespace SistemaReservasLaboratorios.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthService _authService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(AuthService authService, ILogger<LoginController> logger)
        {
            _authService = authService;
            _logger = logger;
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

            // Recorta espacios del usuario y limita longitudes antes de consultar la BD
            nombreUsuario = nombreUsuario.Trim();

            if (nombreUsuario.Length > 50)
            {
                ViewBag.Error = "El nombre de usuario no puede superar los 50 caracteres.";
                return View();
            }

            if (password.Length > 100)
            {
                ViewBag.Error = "La contraseña no puede superar los 100 caracteres.";
                return View();
            }

            Usuario? usuario;
            try
            {
                usuario = _authService.ValidarCredenciales(nombreUsuario, password);
            }
            catch (Exception ex)
            {
                // No se registra la contraseña; solo el error técnico para diagnóstico
                _logger.LogError(ex, "Error al validar credenciales en el login.");
                ViewBag.Error = "Ocurrió un error al iniciar sesión. Intente de nuevo más tarde.";
                return View();
            }

            if (usuario == null)
            {
                // Mismo mensaje para usuario inexistente y contraseña incorrecta
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