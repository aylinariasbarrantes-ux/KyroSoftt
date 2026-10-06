using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaReservasLaboratorios.Controllers;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using SistemaReservasLaboratorios.Tests.Helpers;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Controllers
{
    public class LoginControllerTests
    {
        // ---------- Métodos de apoyo ----------

        private static AppDbContext CrearContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static void AgregarUsuario(AppDbContext context, string nombre, string password, string rol)
        {
            var hasher = new PasswordHasher<Usuario>();
            var usuario = new Usuario { NombreUsuario = nombre, Rol = rol };
            usuario.PasswordHash = hasher.HashPassword(usuario, password);
            context.Usuarios.Add(usuario);
            context.SaveChanges();
        }

        private static LoginController CrearControlador(AppDbContext context)
        {
            var authService = new AuthService(context, new PasswordHasher<Usuario>());
            var controller = new LoginController(authService, Mock.Of<ILogger<LoginController>>());

            var httpContext = new DefaultHttpContext { Session = new FakeSession() };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        // ---------- GET Index ----------

        [Fact]
        public void Index_Get_SinSesion_RetornaVista()
        {
            // Arrange
            using var context = CrearContexto();
            var controller = CrearControlador(context);

            // Act
            var resultado = controller.Index();

            // Assert
            Assert.IsType<ViewResult>(resultado);
        }

        [Fact]
        public void Index_Get_ConSesionActiva_RedirigeAHome()
        {
            // Arrange
            using var context = CrearContexto();
            var controller = CrearControlador(context);
            controller.HttpContext.Session.SetString("NombreUsuario", "admin");

            // Act
            var resultado = controller.Index();

            // Assert
            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redireccion.ActionName);
            Assert.Equal("Home", redireccion.ControllerName);
        }

        // ---------- POST Index: positivas ----------

        [Fact]
        public void Index_Post_ConCredencialesValidas_GuardaSesionYRedirigeAHome()
        {
            // Arrange
            using var context = CrearContexto();
            AgregarUsuario(context, "admin", "admin123", "Administrador");
            var controller = CrearControlador(context);

            // Act
            var resultado = controller.Index("admin", "admin123");

            // Assert
            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redireccion.ActionName);
            Assert.Equal("Home", redireccion.ControllerName);
            Assert.Equal("admin", controller.HttpContext.Session.GetString("NombreUsuario"));
            Assert.Equal("Administrador", controller.HttpContext.Session.GetString("Rol"));
            Assert.Equal("Inicio de sesión exitoso.", controller.TempData["ToastSuccess"]);
        }

        [Fact]
        public void Index_Post_ConEspaciosEnUsuario_RecortaEIniciaSesion()
        {
            // Arrange
            using var context = CrearContexto();
            AgregarUsuario(context, "usuario1", "user123", "Usuario");
            var controller = CrearControlador(context);

            // Act
            var resultado = controller.Index("  usuario1  ", "user123");

            // Assert
            Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Usuario", controller.HttpContext.Session.GetString("Rol"));
        }

        [Fact]
        public void Logout_LimpiaSesionYRedirigeALogin()
        {
            // Arrange
            using var context = CrearContexto();
            var controller = CrearControlador(context);
            controller.HttpContext.Session.SetString("NombreUsuario", "admin");
            controller.HttpContext.Session.SetString("Rol", "Administrador");

            // Act
            var resultado = controller.Logout();

            // Assert
            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redireccion.ActionName);
            Assert.Null(controller.HttpContext.Session.GetString("NombreUsuario"));
            Assert.Null(controller.HttpContext.Session.GetString("Rol"));
        }

        // ---------- POST Index: negativas ----------

        [Theory]
        [InlineData("", "admin123")]
        [InlineData("   ", "admin123")]
        [InlineData(null, "admin123")]
        [InlineData("admin", "")]
        [InlineData("admin", "   ")]
        [InlineData("admin", null)]
        public void Index_Post_ConCamposVacios_RetornaVistaConMensaje(string? usuario, string? password)
        {
            // Arrange
            using var context = CrearContexto();
            var controller = CrearControlador(context);

            // Act
            var resultado = controller.Index(usuario!, password!);

            // Assert
            Assert.IsType<ViewResult>(resultado);
            Assert.Equal("Debe ingresar usuario y contraseña.", controller.TempData["ToastError"]);
            Assert.Null(controller.HttpContext.Session.GetString("NombreUsuario"));
        }

        [Fact]
        public void Index_Post_ConUsuarioMayorA50Caracteres_RetornaVistaConMensaje()
        {
            // Arrange
            using var context = CrearContexto();
            var controller = CrearControlador(context);
            var usuarioLargo = new string('a', 51);

            // Act
            var resultado = controller.Index(usuarioLargo, "admin123");

            // Assert
            Assert.IsType<ViewResult>(resultado);
            Assert.Equal("El nombre de usuario no puede superar los 50 caracteres.", controller.TempData["ToastError"]);
        }

        [Fact]
        public void Index_Post_ConPasswordMayorA100Caracteres_RetornaVistaConMensaje()
        {
            // Arrange
            using var context = CrearContexto();
            var controller = CrearControlador(context);
            var passwordLarga = new string('x', 101);

            // Act
            var resultado = controller.Index("admin", passwordLarga);

            // Assert
            Assert.IsType<ViewResult>(resultado);
            Assert.Equal("La contraseña no puede superar los 100 caracteres.", controller.TempData["ToastError"]);
        }

        [Fact]
        public void Index_Post_ConPasswordIncorrecta_RetornaVistaSinCrearSesion()
        {
            // Arrange
            using var context = CrearContexto();
            AgregarUsuario(context, "admin", "admin123", "Administrador");
            var controller = CrearControlador(context);

            // Act
            var resultado = controller.Index("admin", "contraseña-mala");

            // Assert
            Assert.IsType<ViewResult>(resultado);
            Assert.Equal("Usuario o contraseña incorrectos.", controller.TempData["ToastError"]);
            Assert.Null(controller.HttpContext.Session.GetString("NombreUsuario"));
        }

        [Fact]
        public void Index_Post_ConUsuarioInexistente_MuestraMismoMensajeGenerico()
        {
            // Arrange
            using var context = CrearContexto();
            var controller = CrearControlador(context);

            // Act
            var resultado = controller.Index("noexiste", "admin123");

            // Assert
            Assert.IsType<ViewResult>(resultado);
            Assert.Equal("Usuario o contraseña incorrectos.", controller.TempData["ToastError"]);
        }

        // ---------- POST Index: excepción ----------

        [Fact]
        public void Index_Post_CuandoElServicioFalla_MuestraErrorGenericoSinPropagarExcepcion()
        {
            // Arrange
            var context = CrearContexto();
            var controller = CrearControlador(context);
            context.Dispose(); // Al liberar el contexto, la consulta del servicio lanza excepción

            // Act
            var resultado = controller.Index("admin", "admin123");

            // Assert
            Assert.IsType<ViewResult>(resultado);
            Assert.Equal("Ocurrió un error al iniciar sesión. Intente de nuevo más tarde.", controller.TempData["ToastError"]);
            Assert.Null(controller.HttpContext.Session.GetString("NombreUsuario"));
        }
    }
}