
using Microsoft.AspNetCore.Mvc;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;

namespace SistemaReservasLaboratorios.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthService _authService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            AuthService authService,
            ILogger<LoginController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("NombreUsuario")))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(
            string nombreUsuario,
            string password)
        {
            // Conserva el usuario escrito en el formulario.
            ViewBag.NombreUsuario = nombreUsuario;

            // HU1: Validar campos con las reglas unitarias.
            var error = ReglasLogin.ValidarCampos(
                nombreUsuario,
                password);

            if (error != null)
            {
                TempData["ToastTitle"] =
                    error == "Debe ingresar usuario y contraseña."
                        ? "Campos requeridos"
                        : "Máximo de caracteres";

                TempData["ToastError"] = error;

                return View();
            }

            // HU1: Normalizar el nombre antes de consultar la BD.
            nombreUsuario = ReglasLogin.NormalizarUsuario(
                nombreUsuario);

            Usuario? usuario;

            try
            {
                usuario = _authService.ValidarCredenciales(
                    nombreUsuario,
                    password);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al validar credenciales en el login.");

                TempData["ToastTitle"] = "Error";
                TempData["ToastError"] =
                    "Ocurrió un error al iniciar sesión. Intente de nuevo más tarde.";

                return View();
            }

            if (usuario == null)
            {
                TempData["ToastTitle"] =
                    "No se pudo iniciar sesión";

                TempData["ToastError"] =
                    "Usuario o contraseña incorrectos.";

                return View();
            }

            HttpContext.Session.SetString(
                "NombreUsuario",
                usuario.NombreUsuario);

            HttpContext.Session.SetString(
                "Rol",
                usuario.Rol);

            TempData["ToastTitle"] =
                $"Bienvenido(a), {usuario.NombreUsuario}";

            TempData["ToastSuccess"] =
                "Inicio de sesión exitoso.";

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["ToastTitle"] =
                "Gracias, vuelva pronto";

            TempData["ToastSuccess"] =
                "Sesión cerrada correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}
