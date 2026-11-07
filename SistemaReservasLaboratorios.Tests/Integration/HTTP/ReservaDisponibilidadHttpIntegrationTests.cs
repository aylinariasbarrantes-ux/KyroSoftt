
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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

namespace SistemaReservasLaboratorios.Tests.Integration.HTTP
{
    public class ReservaDisponibilidadHttpIntegrationTests
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
            WebApplicationFactory<Program> factory,
            bool agregarReserva = false)
        {
            using var scope = factory.Services.CreateScope();

            var contexto = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var hasher = scope.ServiceProvider
                .GetRequiredService<IPasswordHasher<Usuario>>();

            contexto.Database.EnsureCreated();

            var usuario = new Usuario
            {
                NombreUsuario = "usuario_hu3",
                Rol = "Usuario"
            };

            usuario.PasswordHash = hasher.HashPassword(
                usuario, "ClavePrueba123!");

            contexto.Usuarios.Add(usuario);

            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio HU3",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            if (agregarReserva)
            {
                contexto.Reservas.Add(new Reserva
                {
                    LaboratorioId = laboratorio.Id,
                    Fecha = DateOnly.FromDateTime(
                        DateTime.Today.AddDays(7)),
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFin = new TimeOnly(11, 0),
                    Responsable = "usuario_hu3",
                    Estado = "Activa"
                });

                contexto.SaveChanges();
            }

            return laboratorio.Id;
        }

        private static async Task IniciarSesion(HttpClient cliente)
        {
            var respuestaGet = await cliente.GetAsync("/Login/Index");

            Assert.Equal(HttpStatusCode.OK, respuestaGet.StatusCode);

            var html = await respuestaGet.Content.ReadAsStringAsync();

            var etiqueta = Regex.Match(
                html,
                "<input[^>]*name=[\"']__RequestVerificationToken[\"'][^>]*>",
                RegexOptions.IgnoreCase);

            Assert.True(etiqueta.Success);

            var valor = Regex.Match(
                etiqueta.Value,
                "value=[\"']([^\"']+)[\"']",
                RegexOptions.IgnoreCase);

            Assert.True(valor.Success);

            using var contenido = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["nombreUsuario"] = "usuario_hu3",
                    ["password"] = "ClavePrueba123!",
                    ["__RequestVerificationToken"] = valor.Groups[1].Value
                });

            var respuestaPost = await cliente.PostAsync(
                "/Login/Index", contenido);

            Assert.Equal(
                HttpStatusCode.Redirect,
                respuestaPost.StatusCode);
        }

        private static string CrearUrl(
            int laboratorioId,
            string horaInicio,
            string horaFin)
        {
            var fecha = DateOnly.FromDateTime(
                DateTime.Today.AddDays(7));

            return "/Reserva/ConsultarDisponibilidad"
                + $"?laboratorioId={laboratorioId}"
                + $"&fecha={fecha:yyyy-MM-dd}"
                + $"&horaInicio={horaInicio}"
                + $"&horaFin={horaFin}";
        }

        [Fact]
        public async Task ConsultarDisponibilidad_SinSesion_RedirigeAlLogin()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var respuesta = await cliente.GetAsync(
                CrearUrl(1, "09:00", "11:00"));

            Assert.Equal(
                HttpStatusCode.Redirect,
                respuesta.StatusCode);

            Assert.NotNull(respuesta.Headers.Location);
            Assert.Contains(
                "/Login",
                respuesta.Headers.Location!.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ConsultarDisponibilidad_Libre_RetornaDisponible()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            await IniciarSesion(cliente);

            var respuesta = await cliente.GetAsync(
                CrearUrl(laboratorioId, "09:00", "11:00"));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

            using var json = JsonDocument.Parse(
                await respuesta.Content.ReadAsStringAsync());

            Assert.True(
                json.RootElement.GetProperty("disponible").GetBoolean());
        }

        [Fact]
        public async Task ConsultarDisponibilidad_Ocupado_RetornaNoDisponible()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory, true);
            await IniciarSesion(cliente);

            var respuesta = await cliente.GetAsync(
                CrearUrl(laboratorioId, "10:00", "12:00"));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

            using var json = JsonDocument.Parse(
                await respuesta.Content.ReadAsStringAsync());

            Assert.False(
                json.RootElement.GetProperty("disponible").GetBoolean());
        }

        [Fact]
        public async Task ConsultarDisponibilidad_FormatoInvalido_RetornaBadRequest()
        {
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            var laboratorioId = PrepararDatos(factory);
            await IniciarSesion(cliente);

            var respuesta = await cliente.GetAsync(
                CrearUrl(laboratorioId, "09:15", "11:00"));

            Assert.Equal(
                HttpStatusCode.BadRequest,
                respuesta.StatusCode);
        }
    }
}
