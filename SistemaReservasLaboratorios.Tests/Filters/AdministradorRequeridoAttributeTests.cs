using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using SistemaReservasLaboratorios.Filters;
using SistemaReservasLaboratorios.Tests.Helpers;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Filters
{
    public class AdministradorRequeridoAttributeTests
    {
        // ---------- Métodos de apoyo ----------

        private static ActionExecutingContext CrearContexto(HttpContext httpContext)
        {
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: new object());
        }

        private static DefaultHttpContext CrearHttpContext(string? nombreUsuario, string? rol)
        {
            var httpContext = new DefaultHttpContext { Session = new FakeSession() };
            if (nombreUsuario != null) httpContext.Session.SetString("NombreUsuario", nombreUsuario);
            if (rol != null) httpContext.Session.SetString("Rol", rol);
            return httpContext;
        }

        // ---------- Positivas ----------

        [Theory]
        [InlineData("Administrador")]
        [InlineData("administrador")]
        [InlineData("ADMINISTRADOR")]
        public void OnActionExecuting_ConRolAdministrador_NoCortaLaAccion(string rol)
        {
            // Arrange
            var context = CrearContexto(CrearHttpContext("admin", rol));
            var filtro = new AdministradorRequeridoAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        // ---------- Negativas ----------

        [Fact]
        public void OnActionExecuting_SinSesion_RedirigeALogin()
        {
            // Arrange
            var context = CrearContexto(CrearHttpContext(null, null));
            var filtro = new AdministradorRequeridoAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            var redireccion = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("Index", redireccion.ActionName);
            Assert.Equal("Login", redireccion.ControllerName);
        }

        [Fact]
        public void OnActionExecuting_ConRolUsuario_RedirigeAHome()
        {
            // Arrange
            var context = CrearContexto(CrearHttpContext("usuario1", "Usuario"));
            var filtro = new AdministradorRequeridoAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            var redireccion = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("Index", redireccion.ActionName);
            Assert.Equal("Home", redireccion.ControllerName);
        }

        [Fact]
        public void OnActionExecuting_ConSesionPeroSinRol_RedirigeAHome()
        {
            // Arrange
            var context = CrearContexto(CrearHttpContext("usuario1", null));
            var filtro = new AdministradorRequeridoAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            var redireccion = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("Home", redireccion.ControllerName);
        }

        // ---------- Excepción ----------

        [Fact]
        public void OnActionExecuting_SinSesionConfigurada_LanzaInvalidOperationException()
        {
            // Arrange: sin Session asignada, simula que falta app.UseSession()
            var context = CrearContexto(new DefaultHttpContext());
            var filtro = new AdministradorRequeridoAttribute();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => filtro.OnActionExecuting(context));
        }
    }
}