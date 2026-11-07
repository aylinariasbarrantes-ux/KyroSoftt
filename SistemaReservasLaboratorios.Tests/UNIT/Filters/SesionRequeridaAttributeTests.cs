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
    public class SesionRequeridaAttributeTests
    {
        // ---------- Método de apoyo ----------

        private static ActionExecutingContext CrearContexto(HttpContext httpContext)
        {
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: new object());
        }

        private static DefaultHttpContext CrearHttpContextConSesion(string? nombreUsuario = null)
        {
            var httpContext = new DefaultHttpContext { Session = new FakeSession() };
            if (nombreUsuario != null)
            {
                httpContext.Session.SetString("NombreUsuario", nombreUsuario);
            }
            return httpContext;
        }

        // ---------- Positivas ----------

        [Fact]
        public void OnActionExecuting_ConSesionActiva_NoCortaLaAccion()
        {
            // Arrange
            var httpContext = CrearHttpContextConSesion("admin");
            var context = CrearContexto(httpContext);
            var filtro = new SesionRequeridaAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_AgregaHeadersDeNoCache()
        {
            // Arrange
            var httpContext = CrearHttpContextConSesion("admin");
            var context = CrearContexto(httpContext);
            var filtro = new SesionRequeridaAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            Assert.Equal("no-store, no-cache, must-revalidate", httpContext.Response.Headers.CacheControl.ToString());
            Assert.Equal("no-cache", httpContext.Response.Headers.Pragma.ToString());
            Assert.Equal("0", httpContext.Response.Headers.Expires.ToString());
        }

        // ---------- Negativas ----------

        [Fact]
        public void OnActionExecuting_SinSesion_RedirigeALogin()
        {
            // Arrange
            var httpContext = CrearHttpContextConSesion();
            var context = CrearContexto(httpContext);
            var filtro = new SesionRequeridaAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            var redireccion = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("Index", redireccion.ActionName);
            Assert.Equal("Login", redireccion.ControllerName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void OnActionExecuting_ConNombreUsuarioVacio_RedirigeALogin(string nombreUsuario)
        {
            // Arrange
            var httpContext = CrearHttpContextConSesion(nombreUsuario);
            var context = CrearContexto(httpContext);
            var filtro = new SesionRequeridaAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            Assert.IsType<RedirectToActionResult>(context.Result);
        }

        [Fact]
        public void OnActionExecuting_SinSesion_TambienAgregaHeadersDeNoCache()
        {
            // Arrange
            var httpContext = CrearHttpContextConSesion();
            var context = CrearContexto(httpContext);
            var filtro = new SesionRequeridaAttribute();

            // Act
            filtro.OnActionExecuting(context);

            // Assert
            Assert.Equal("no-store, no-cache, must-revalidate", httpContext.Response.Headers.CacheControl.ToString());
        }

        // ---------- Excepción ----------

        [Fact]
        public void OnActionExecuting_SinSesionConfigurada_LanzaInvalidOperationException()
        {
            // Arrange: DefaultHttpContext sin Session asignada simula que falta app.UseSession()
            var httpContext = new DefaultHttpContext();
            var context = CrearContexto(httpContext);
            var filtro = new SesionRequeridaAttribute();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => filtro.OnActionExecuting(context));
        }
    }
}