
using System;
using System.Collections.Generic;
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
    public class MisReservasHttpIntegrationTests
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

            contexto.Database.EnsureCreated();

            var hasher = scope.ServiceProvider
                .GetRequiredService<IPasswordHasher<Usuario>>();

            var usuario = new Usuario
            {
                NombreUsuario = "usuario_hu7",
                Rol = "Usuario"
            };

            usuario.PasswordHash = hasher.HashPassword(
                usuario, "ClavePrueba123!");

            contexto.Usuarios.Add(usuario);

            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio HU7 Propio",
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
            string responsable,
            int diasDesdeHoy,
            string estado = "Activa")
        {
            using var scope = factory.Services.CreateScope();

            var contexto = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var reserva = new Reserva
            {
                LaboratorioId = laboratorioId,
                Responsable = responsable,
                Fecha = DateOnly.FromDateTime(
                    DateTime.Today.AddDays(diasDesdeHoy)),
                HoraInicio = new TimeOnly(9, 0),
                HoraFin = new TimeOnly(11, 0),
                Estado = estado
            };

            contexto.Reservas.Add(reserva);
            contexto.SaveChanges();

            return reserva.Id;
        }

        private static async Task IniciarSesion(HttpClient cliente)
        {
            var respuestaLogin = await cliente.GetAsync(
                "/Login/Index");

            Assert.Equal(
                HttpStatusCode.OK,
                respuestaLogin.StatusCode);

            var html = await respuestaLogin.Content
                .ReadAsStringAsync();

            var etiqueta = Regex.Match(
                html,
                "<input[^>]*name=[\"']__RequestVerificationToken[\"'][^>]*>",
                RegexOptions.IgnoreCase);

            Assert.True(
                etiqueta.Success,
                "No se encontró el token antifalsificación.");

            var coincidencia = Regex.Match(
                etiqueta.Value,
                "value=[\"']([^\"']+)[\"']",
                RegexOptions.IgnoreCase);

            Assert.True(
                coincidencia.Success,
                "No se encontró el valor del token.");

            var token = WebUtility.HtmlDecode(
                coincidencia.Groups[1].Value);

            using var formulario = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["nombreUsuario"] = "usuario_hu7",
                    ["password"] = "ClavePrueba123!",
                    ["__RequestVerificationToken"] = token
                });

            var respuesta = await cliente.PostAsync(
                "/Login/Index",
                formulario);

            Assert.Equal(
                HttpStatusCode.Redirect,
                respuesta.StatusCode);

            Assert.NotNull(respuesta.Headers.Location);

            var ruta = respuesta.Headers.Location!
                .ToString()
                .TrimEnd('/');

            Assert.True(
                ruta == "" ||
                ruta.Equals(
                    "/Home",
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Equals(
                    "/Home/Index",
                    StringComparison.OrdinalIgnoreCase),
                $"Redirección inesperada: {ruta}");
        }

        // HU7 HTTP-01:
        // Sin sesión no se permite consultar Mis reservas.

        [Fact]
        public async Task MisReservas_SinSesion_NoPermiteAcceso()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var respuesta = await cliente.GetAsync(
                "/Reserva/MisReservas");

            Assert.NotEqual(
                HttpStatusCode.OK,
                respuesta.StatusCode);
        }

        // HU7 HTTP-02:
        // Usuario autenticado sin reservas ve el mensaje vacío.

        [Fact]
        public async Task MisReservas_SinReservas_MuestraMensajeVacio()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            PrepararDatos(factory);
            await IniciarSesion(cliente);

            var respuesta = await cliente.GetAsync(
                "/Reserva/MisReservas");

            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            var html = await respuesta.Content.ReadAsStringAsync();

            Assert.Contains(
                "Todavía no tienes reservas",
                html);

            Assert.Contains(
                "0 reserva(s) asociada(s)",
                html);
        }

        // HU7 HTTP-03:
        // El usuario ve únicamente sus propias reservas.

        [Fact]
        public async Task MisReservas_UsuariosDistintos_MuestraSoloPropias()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);

            var reservaPropia = AgregarReserva(
                factory,
                laboratorioId,
                "usuario_hu7",
                7);

            var reservaAjena = AgregarReserva(
                factory,
                laboratorioId,
                "otro_usuario",
                8);

            await IniciarSesion(cliente);

            var respuesta = await cliente.GetAsync(
                "/Reserva/MisReservas");

            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            var html = await respuesta.Content.ReadAsStringAsync();

            Assert.Contains(
                $"data-reservation-row=\"{reservaPropia}\"",
                html);

            Assert.DoesNotContain(
                $"data-reservation-row=\"{reservaAjena}\"",
                html);

            Assert.Contains(
                "1 reserva(s) asociada(s)",
                html);
        }

        // HU7 HTTP-04:
        // Una reserva activa futura muestra el botón de cancelar.

        [Fact]
        public async Task MisReservas_ReservaActiva_MuestraBotonCancelar()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);

            var reservaId = AgregarReserva(
                factory,
                laboratorioId,
                "usuario_hu7",
                7);

            await IniciarSesion(cliente);

            var respuesta = await cliente.GetAsync(
                "/Reserva/MisReservas");

            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            var html = await respuesta.Content.ReadAsStringAsync();

            Assert.Contains(
                $"data-cancel-reservation=\"{reservaId}\"",
                html);

            Assert.Contains(
                "id=\"activeReservationsCount\">1</span>",
                html);
        }

        // HU7 HTTP-05:
        // Una reserva cancelada se muestra en el historial,
        // pero no tiene botón para volver a cancelarla.

        [Fact]
        public async Task MisReservas_ReservaCancelada_NoMuestraBotonCancelar()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);

            var reservaId = AgregarReserva(
                factory,
                laboratorioId,
                "usuario_hu7",
                7,
                "Cancelada");

            await IniciarSesion(cliente);

            var respuesta = await cliente.GetAsync(
                "/Reserva/MisReservas");

            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            var html = await respuesta.Content.ReadAsStringAsync();

            Assert.Contains(
                $"data-reservation-row=\"{reservaId}\"",
                html);

            Assert.DoesNotContain(
                $"data-cancel-reservation=\"{reservaId}\"",
                html);

            Assert.Contains(
                "id=\"cancelledReservationsCount\">1</span>",
                html);
        }

        // HU7 HTTP-06:
        // Una reserva pasada aparece como expirada.

        [Fact]
        public async Task MisReservas_ReservaPasada_MuestraEstadoExpirada()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);

            var reservaId = AgregarReserva(
                factory,
                laboratorioId,
                "usuario_hu7",
                -7);

            await IniciarSesion(cliente);

            var respuesta = await cliente.GetAsync(
                "/Reserva/MisReservas");

            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            var html = await respuesta.Content.ReadAsStringAsync();

            Assert.Contains(
                $"data-reservation-row=\"{reservaId}\"",
                html);

            Assert.Contains(
                "id=\"expiredReservationsCount\">1</span>",
                html);

            Assert.DoesNotContain(
                $"data-cancel-reservation=\"{reservaId}\"",
                html);
        }
    }
}
