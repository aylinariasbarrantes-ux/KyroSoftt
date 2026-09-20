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
            if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("NombreUsuario")))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: procesa el formulario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string nombreUsuario, string password)
        {
            // Conserva el nombre de usuario si la validación falla, sin volver a mostrar la contraseña.
            ViewBag.NombreUsuario = nombreUsuario;

            // Validar que los campos no estén vacíos
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ToastError"] = "Debe ingresar usuario y contraseña.";
                return View();
            }

            // Recorta espacios del usuario y limita longitudes antes de consultar la BD
            nombreUsuario = nombreUsuario.Trim();

            if (nombreUsuario.Length > 50)
            {
                TempData["ToastError"] = "El nombre de usuario no puede superar los 50 caracteres.";
                return View();
            }

            if (password.Length > 100)
            {
                TempData["ToastError"] = "La contraseña no puede superar los 100 caracteres.";
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
                TempData["ToastError"] = "Ocurrió un error al iniciar sesión. Intente de nuevo más tarde.";
                return View();
            }

            if (usuario == null)
            {
                // Mismo mensaje para usuario inexistente y contraseña incorrecta
                TempData["ToastError"] = "Usuario o contraseña incorrectos.";
                return View();
            }

            // Guardar al usuario en sesión para identificarlo durante el uso de la app
            HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);
            HttpContext.Session.SetString("Rol", usuario.Rol);

            TempData["ToastSuccess"] = $"Inicio de sesión exitoso. Bienvenido, {usuario.NombreUsuario}.";

            // Redirige según el rol (por ahora al Home, luego pueden diferenciarlo)
            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["ToastSuccess"] = "Sesión cerrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}