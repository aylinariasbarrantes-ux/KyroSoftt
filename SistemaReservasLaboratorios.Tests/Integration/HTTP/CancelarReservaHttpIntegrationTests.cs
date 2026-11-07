
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Integration.HTTP
{
    public class CancelarReservaHttpIntegrationTests
    {
        private const string Usuario = "usuario_hu8";
        private const string OtroUsuario = "otro_hu8";
        private const string Password = "ClavePrueba123!";

        private static WebApplicationFactory<Program> CrearFactory()
        {
            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<AppDbContext>>();

                        var nombreBD = Guid.NewGuid().ToString();

                        services.AddDbContext<AppDbContext>(options =>
                            options.UseInMemoryDatabase(nombreBD));
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

            var contexto =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();

            contexto.Database.EnsureCreated();

            var hasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher<Usuario>>();

            foreach (var nombre in new[] { Usuario, OtroUsuario })
            {
                if (!contexto.Usuarios.Any(
                    u => u.NombreUsuario == nombre))
                {
                    var usuario = new Usuario
                    {
                        NombreUsuario = nombre,
                        Rol = "Usuario"
                    };

                    usuario.PasswordHash =
                        hasher.HashPassword(usuario, Password);

                    contexto.Usuarios.Add(usuario);
                }
            }

            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio HU8 HTTP",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio.Id;
        }

        private static int AgregarReserva(
            WebApplicationFactory<Program> factory,
            int laboratorioId,
            string responsable = Usuario,
            string estado = "Activa")
        {
            using var scope = factory.Services.CreateScope();

            var contexto =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var reserva = new Reserva
            {
                LaboratorioId = laboratorioId,
                Responsable = responsable,
                Fecha = DateOnly.FromDateTime(
                    DateTime.Today.AddDays(7)),
                HoraInicio = new TimeOnly(9, 0),
                HoraFin = new TimeOnly(11, 0),
                Estado = estado
            };

            contexto.Reservas.Add(reserva);
            contexto.SaveChanges();

            return reserva.Id;
        }

        private static string ObtenerEstado(
            WebApplicationFactory<Program> factory,
            int reservaId)
        {
            using var scope = factory.Services.CreateScope();

            var contexto =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return contexto.Reservas
                .AsNoTracking()
                .Single(r => r.Id == reservaId)
                .Estado;
        }

        private static async Task<string>
            ObtenerToken(HttpClient cliente, string ruta)
        {
            var respuesta = await cliente.GetAsync(ruta);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

            var html = await respuesta.Content.ReadAsStringAsync();

            var coincidencia = Regex.Match(
                html,
                "<input[^>]*name=[\"']__RequestVerificationToken[\"'][^>]*>",
                RegexOptions.IgnoreCase);

            Assert.True(
                coincidencia.Success,
                "No se encontró el campo antifalsificación en " + ruta);

            var valor = Regex.Match(
                coincidencia.Value,
                "value=[\"']([^\"']+)[\"']",
                RegexOptions.IgnoreCase);

            Assert.True(
                valor.Success,
                "No se encontró el valor del token antifalsificación.");

            return WebUtility.HtmlDecode(valor.Groups[1].Value);
        }

        private static async Task
            IniciarSesion(
                HttpClient cliente,
                string nombreUsuario = Usuario)
        {
            var token = await ObtenerToken(
                cliente, "/Login/Index");

            using var formulario = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["nombreUsuario"] = nombreUsuario,
                    ["password"] = Password,
                    ["__RequestVerificationToken"] = token
                });

            var respuesta = await cliente.PostAsync(
                "/Login/Index", formulario);

            Assert.True(
                respuesta.StatusCode == HttpStatusCode.Redirect ||
                respuesta.StatusCode == HttpStatusCode.Found,
                "El inicio de sesión no produjo la redirección esperada.");
        }

        private static async Task<HttpResponseMessage>
            EnviarCancelacion(
                HttpClient cliente,
                int reservaId,
                string? token)
        {
            var datos = new Dictionary<string, string>
            {
                ["reservaId"] = reservaId.ToString()
            };

            if (token != null)
            {
                datos["__RequestVerificationToken"] = token;
            }

            using var formulario = new FormUrlEncodedContent(datos);

            return await cliente.PostAsync(
                "/Reserva/CancelarReserva",
                formulario);
        }

        // HU8-21: Sin sesión no se permite cancelar.

        [Fact]
        public async Task
            CancelarReserva_SinSesion_NoPermiteOperacion()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var reservaId = AgregarReserva(factory, laboratorioId);

            using var respuesta = await EnviarCancelacion(
                cliente, reservaId, null);

            Assert.NotEqual(
                HttpStatusCode.OK, respuesta.StatusCode);

            Assert.Equal(
                "Activa", ObtenerEstado(factory, reservaId));
        }

        // HU8-22: El propietario cancela mediante HTTP.

        [Fact]
        public async Task
            CancelarReserva_Propietario_DevuelveOk()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var reservaId = AgregarReserva(factory, laboratorioId);

            await IniciarSesion(cliente);

            var token = await ObtenerToken(
                cliente, "/Reserva/MisReservas");

            using var respuesta = await EnviarCancelacion(
                cliente, reservaId, token);

            Assert.Equal(
                HttpStatusCode.OK, respuesta.StatusCode);

            Assert.Equal(
                "Cancelada", ObtenerEstado(factory, reservaId));
        }

        // HU8-23: Un usuario no cancela reservas ajenas.

        [Fact]
        public async Task
            CancelarReserva_ReservaAjena_DevuelveBadRequest()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);

            var reservaId = AgregarReserva(
                factory, laboratorioId, OtroUsuario);

            await IniciarSesion(cliente);

            var token = await ObtenerToken(
                cliente, "/Reserva/MisReservas");

            using var respuesta = await EnviarCancelacion(
                cliente, reservaId, token);

            Assert.Equal(
                HttpStatusCode.BadRequest, respuesta.StatusCode);

            Assert.Equal(
                "Activa", ObtenerEstado(factory, reservaId));
        }

        // HU8-24: Una reserva inexistente devuelve BadRequest.

        [Fact]
        public async Task
            CancelarReserva_IdInexistente_DevuelveBadRequest()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            PrepararDatos(factory);

            await IniciarSesion(cliente);

            var token = await ObtenerToken(
                cliente, "/Reserva/MisReservas");

            using var respuesta = await EnviarCancelacion(
                cliente, 99999, token);

            Assert.Equal(
                HttpStatusCode.BadRequest, respuesta.StatusCode);
        }

        // HU8-25: No se permite cancelar dos veces.

        [Fact]
        public async Task
            CancelarReserva_DobleCancelacion_SegundaDevuelveBadRequest()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var reservaId = AgregarReserva(factory, laboratorioId);

            await IniciarSesion(cliente);

            var token = await ObtenerToken(
                cliente, "/Reserva/MisReservas");

            using var primera = await EnviarCancelacion(
                cliente, reservaId, token);

            Assert.Equal(
                HttpStatusCode.OK, primera.StatusCode);

            using var segunda = await EnviarCancelacion(
                cliente, reservaId, token);

            Assert.Equal(
                HttpStatusCode.BadRequest, segunda.StatusCode);

            Assert.Equal(
                "Cancelada", ObtenerEstado(factory, reservaId));
        }

        // HU8-26: Una solicitud sin token debe rechazarse.

        [Fact]
        public async Task
            CancelarReserva_SinTokenAntifalsificacion_NoCancela()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            var reservaId = AgregarReserva(factory, laboratorioId);

            await IniciarSesion(cliente);

            using var respuesta = await EnviarCancelacion(
                cliente, reservaId, null);

            Assert.Equal(
                HttpStatusCode.BadRequest, respuesta.StatusCode);

            Assert.Equal(
                "Activa", ObtenerEstado(factory, reservaId));
        }
    }
}
