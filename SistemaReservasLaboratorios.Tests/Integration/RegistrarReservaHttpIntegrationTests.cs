
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Integration
{
    public class RegistrarReservaHttpIntegrationTests
    {
        private static WebApplicationFactory<Program> CrearFactory()
        {
            var nombreBase = Guid.NewGuid().ToString();

            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<AppDbContext>>();

                        services.AddDbContext<AppDbContext>(options =>
                            options.UseInMemoryDatabase(nombreBase));
                    });
                });
        }

        private static HttpClient CrearCliente(
            WebApplicationFactory<Program> factory)
        {
            return factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = true
                });
        }

        private static int PrepararDatos(
            WebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();

            var contexto = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var hasher = scope.ServiceProvider
                .GetRequiredService<IPasswordHasher<Usuario>>();

            contexto.Database.EnsureCreated();

            var usuario = new Usuario
            {
                NombreUsuario = "usuario_hu4",
                Rol = "Usuario"
            };

            usuario.PasswordHash = hasher.HashPassword(
                usuario, "ClavePrueba123!");

            contexto.Usuarios.Add(usuario);

            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio HTTP HU4",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio.Id;
        }

        private static async Task<string> ObtenerToken(
            HttpClient cliente)
        {
            var respuesta = await cliente.GetAsync("/Login/Index");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

            var html = await respuesta.Content.ReadAsStringAsync();

            var etiqueta = Regex.Match(
                html,
                "<input[^>]*name=[\"']__RequestVerificationToken[\"'][^>]*>",
                RegexOptions.IgnoreCase);

            Assert.True(
                etiqueta.Success,
                "No se encontró el campo antifalsificación.");

            var valor = Regex.Match(
                etiqueta.Value,
                "value=[\"']([^\"']+)[\"']",
                RegexOptions.IgnoreCase);

            Assert.True(
                valor.Success,
                "No se encontró el valor del token.");

            return WebUtility.HtmlDecode(valor.Groups[1].Value);
        }

        private static async Task<string> IniciarSesion(
            HttpClient cliente)
        {
            var token = await ObtenerToken(cliente);

            using var contenido = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["nombreUsuario"] = "usuario_hu4",
                    ["password"] = "ClavePrueba123!",
                    ["__RequestVerificationToken"] = token
                });

            var respuesta = await cliente.PostAsync(
                "/Login/Index", contenido);

            Assert.Equal(
                HttpStatusCode.Redirect,
                respuesta.StatusCode);

            Assert.NotNull(respuesta.Headers.Location);

            var rutaRedireccion = respuesta.Headers.Location!
                .ToString()
                .TrimEnd('/');

            Assert.True(
                rutaRedireccion == "" ||
                rutaRedireccion.Equals(
                    "/Home",
                    StringComparison.OrdinalIgnoreCase) ||
                rutaRedireccion.Equals(
                    "/Home/Index",
                    StringComparison.OrdinalIgnoreCase),
                $"Redirección inesperada: {respuesta.Headers.Location}");

            return token;
        }

        private static FormUrlEncodedContent CrearFormulario(
            int laboratorioId,
            string token,
            string horaInicio = "09:00",
            string horaFin = "11:00",
            bool incluirToken = true,
            bool suplantarResponsable = false)
        {
            var fecha = DateOnly.FromDateTime(
                DateTime.Today.AddDays(7));

            var datos = new Dictionary<string, string>
            {
                ["laboratorioId"] = laboratorioId.ToString(),
                ["fecha"] = fecha.ToString("yyyy-MM-dd"),
                ["horaInicio"] = horaInicio,
                ["horaFin"] = horaFin
            };

            if (incluirToken)
            {
                datos["__RequestVerificationToken"] = token;
            }

            if (suplantarResponsable)
            {
                datos["responsable"] = "usuario_falso";
            }

            return new FormUrlEncodedContent(datos);
        }

        private static int ContarReservas(
            WebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();

            var contexto = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            return contexto.Reservas.Count();
        }

        // HU4 - Registro válido

        [Fact]
        public async Task RegistrarReserva_DatosValidos_GuardaMedianteHttp()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var token = await IniciarSesion(cliente);

            using var formulario = CrearFormulario(
                laboratorioId, token);

            var respuesta = await cliente.PostAsync(
                "/Reserva/RegistrarReserva",
                formulario);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal(1, ContarReservas(factory));
        }

        // HU4 - Protección antifalsificación

        [Fact]
        public async Task RegistrarReserva_SinToken_RetornaBadRequest()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var token = await IniciarSesion(cliente);

            using var formulario = CrearFormulario(
                laboratorioId,
                token,
                incluirToken: false);

            var respuesta = await cliente.PostAsync(
                "/Reserva/RegistrarReserva",
                formulario);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                respuesta.StatusCode);

            Assert.Equal(0, ContarReservas(factory));
        }

        // HU4 - Prevención de traslapes

        [Fact]
        public async Task RegistrarReserva_HorarioOcupado_RetornaBadRequest()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var token = await IniciarSesion(cliente);

            using var primeraSolicitud = CrearFormulario(
                laboratorioId, token);

            var primeraRespuesta = await cliente.PostAsync(
                "/Reserva/RegistrarReserva",
                primeraSolicitud);

            Assert.Equal(
                HttpStatusCode.OK,
                primeraRespuesta.StatusCode);

            using var segundaSolicitud = CrearFormulario(
                laboratorioId,
                token,
                "10:00",
                "12:00");

            var segundaRespuesta = await cliente.PostAsync(
                "/Reserva/RegistrarReserva",
                segundaSolicitud);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                segundaRespuesta.StatusCode);

            Assert.Equal(1, ContarReservas(factory));
        }

        // HU4 - Responsable obtenido de la sesión

        [Fact]
        public async Task RegistrarReserva_ResponsableManipulado_UsaUsuarioSesion()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var token = await IniciarSesion(cliente);

            using var formulario = CrearFormulario(
                laboratorioId,
                token,
                suplantarResponsable: true);

            var respuesta = await cliente.PostAsync(
                "/Reserva/RegistrarReserva",
                formulario);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

            using var scope = factory.Services.CreateScope();

            var contexto = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var reserva = Assert.Single(contexto.Reservas);

            Assert.Equal("usuario_hu4", reserva.Responsable);
            Assert.NotEqual("usuario_falso", reserva.Responsable);
        }

        // HU5 - Horarios inválidos
        // Cada InlineData representa una prueba independiente.

        [Theory]
        [InlineData("07:30", "09:00")]
        [InlineData("21:00", "22:30")]
        [InlineData("09:15", "10:00")]
        [InlineData("09:00", "10:45")]
        [InlineData("09:00", "13:30")]
        [InlineData("10:00", "10:00")]
        [InlineData("12:00", "10:00")]
        public async Task RegistrarReserva_HorarioInvalidoHttp_RetornaBadRequest(
            string horaInicio,
            string horaFin)
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var token = await IniciarSesion(cliente);

            using var formulario = CrearFormulario(
                laboratorioId,
                token,
                horaInicio,
                horaFin);

            var respuesta = await cliente.PostAsync(
                "/Reserva/RegistrarReserva",
                formulario);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                respuesta.StatusCode);

            Assert.Equal(0, ContarReservas(factory));
        }

        // HU5 - Horarios válidos en los límites permitidos

        [Theory]
        [InlineData("08:00", "12:00")]
        [InlineData("18:00", "22:00")]
        public async Task RegistrarReserva_HorarioLimiteValidoHttp_GuardaReserva(
            string horaInicio,
            string horaFin)
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var token = await IniciarSesion(cliente);

            using var formulario = CrearFormulario(
                laboratorioId,
                token,
                horaInicio,
                horaFin);

            var respuesta = await cliente.PostAsync(
                "/Reserva/RegistrarReserva",
                formulario);

            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            Assert.Equal(1, ContarReservas(factory));
        }
    }
}
