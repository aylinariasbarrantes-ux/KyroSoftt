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

namespace SistemaReservasLaboratorios.Tests.Integration.HTTP
{
    public class LaboratorioHttpIntegrationTests
    {
        private static WebApplicationFactory<Program> CrearFactory()
        {
            var nombreBaseDatos = Guid.NewGuid().ToString();

            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<
                            DbContextOptions<AppDbContext>>();

                        services.AddDbContext<AppDbContext>(options =>
                            options.UseInMemoryDatabase(
                                nombreBaseDatos));
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

        private static void PrepararDatos(
            WebApplicationFactory<Program> factory,
            string rol,
            params Laboratorio[] laboratorios)
        {
            using var scope = factory.Services.CreateScope();

            var contexto = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var hasher = scope.ServiceProvider
                .GetRequiredService<IPasswordHasher<Usuario>>();

            contexto.Database.EnsureCreated();

            var usuario = new Usuario
            {
                NombreUsuario = "usuario_prueba",
                Rol = rol
            };

            usuario.PasswordHash = hasher.HashPassword(
                usuario,
                "ClavePrueba123!");

            contexto.Usuarios.Add(usuario);

            if (laboratorios.Length > 0)
            {
                contexto.Laboratorios.AddRange(laboratorios);
            }

            contexto.SaveChanges();
        }

        private static async Task IniciarSesion(
            HttpClient cliente)
        {
            // Obtener el formulario y el token antifalsificación.
            var respuestaGet = await cliente.GetAsync(
                "/Login/Index");

            Assert.Equal(
                HttpStatusCode.OK,
                respuestaGet.StatusCode);

            var html = await respuestaGet.Content
                .ReadAsStringAsync();

            var coincidencia = Regex.Match(
                html,
                "<input[^>]*name=[\"']__RequestVerificationToken[\"'][^>]*>",
                RegexOptions.IgnoreCase);

            Assert.True(
                coincidencia.Success,
                "No se encontró el token antifalsificación.");

            var etiqueta = coincidencia.Value;

            var valor = Regex.Match(
                etiqueta,
                "value=[\"']([^\"']+)[\"']",
                RegexOptions.IgnoreCase);

            Assert.True(
                valor.Success,
                "No se encontró el valor del token.");

            var formulario = new Dictionary<string, string>
            {
                ["nombreUsuario"] = "usuario_prueba",
                ["password"] = "ClavePrueba123!",
                ["__RequestVerificationToken"] = valor.Groups[1].Value
            };

            using var contenido = new FormUrlEncodedContent(
                formulario);

            var respuestaPost = await cliente.PostAsync(
                "/Login/Index",
                contenido);

            Assert.Equal(
                HttpStatusCode.Redirect,
                respuestaPost.StatusCode);
        }

        [Fact]
        public async Task Index_SinSesion_RedirigeAlLogin()
        {
            // Arrange
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            // Act
            var respuesta = await cliente.GetAsync(
                "/Laboratorio/Index");

            // Assert
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
        public async Task Index_UsuarioAutenticado_PuedeAcceder()
        {
            // Arrange
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            PrepararDatos(factory, "Usuario");
            await IniciarSesion(cliente);

            // Act
            var respuesta = await cliente.GetAsync(
                "/Laboratorio/Index");

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);
        }

        [Fact]
        public async Task Index_ConLaboratorios_MuestraInformacion()
        {
            // Arrange
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            PrepararDatos(
                factory,
                "Usuario",
                new Laboratorio
                {
                    Nombre = "Laboratorio Central",
                    Ubicacion = "Edificio A",
                    Capacidad = 30,
                    Estado = "Habilitado"
                });

            await IniciarSesion(cliente);

            // Act
            var respuesta = await cliente.GetAsync(
                "/Laboratorio/Index");

            var html = await respuesta.Content
                .ReadAsStringAsync();

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            Assert.Contains("Laboratorio Central", html);
            Assert.Contains("Edificio A", html);
            Assert.Contains("30 personas", html);
            Assert.Contains("Habilitado", html);
        }

        [Fact]
        public async Task Index_SinLaboratorios_MuestraMensaje()
        {
            // Arrange
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            PrepararDatos(factory, "Usuario");
            await IniciarSesion(cliente);

            // Act
            var respuesta = await cliente.GetAsync(
                "/Laboratorio/Index");

            var html = await respuesta.Content
                .ReadAsStringAsync();

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            Assert.Contains(
                "No hay laboratorios registrados",
                html);
        }

        [Fact]
        public async Task Index_LaboratorioHabilitado_MuestraEnlaceReservar()
        {
            // Arrange
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            PrepararDatos(
                factory,
                "Usuario",
                new Laboratorio
                {
                    Nombre = "Laboratorio Disponible",
                    Ubicacion = "Edificio B",
                    Capacidad = 25,
                    Estado = "Habilitado"
                });

            await IniciarSesion(cliente);

            // Act
            var respuesta = await cliente.GetAsync(
                "/Laboratorio/Index");

            var html = await respuesta.Content
                .ReadAsStringAsync();

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            Assert.Contains("Reservar", html);
            Assert.Contains("crearReserva=1", html);
            Assert.Contains("laboratorioId=", html);
        }

        [Fact]
        public async Task Index_LaboratorioFueraServicio_DeshabilitaReserva()
        {
            // Arrange
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            PrepararDatos(
                factory,
                "Usuario",
                new Laboratorio
                {
                    Nombre = "Laboratorio Cerrado",
                    Ubicacion = "Edificio C",
                    Capacidad = 20,
                    Estado = "Fuera de servicio"
                });

            await IniciarSesion(cliente);

            // Act
            var respuesta = await cliente.GetAsync(
                "/Laboratorio/Index");

            var html = await respuesta.Content
                .ReadAsStringAsync();

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            Assert.Contains("Fuera de servicio", html);
            Assert.Contains("disabled", html);
            Assert.DoesNotContain("crearReserva=1", html);
        }

        [Fact]
        public async Task Index_Administrador_NoMuestraAcciones()
        {
            // Arrange
            using var factory = CrearFactory();
            using var cliente = CrearCliente(factory);

            PrepararDatos(
                factory,
                "Administrador",
                new Laboratorio
                {
                    Nombre = "Laboratorio Administrativo",
                    Ubicacion = "Edificio Principal",
                    Capacidad = 40,
                    Estado = "Habilitado"
                });

            await IniciarSesion(cliente);

            // Act
            var respuesta = await cliente.GetAsync(
                "/Laboratorio/Index");

            var html = await respuesta.Content
                .ReadAsStringAsync();

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                respuesta.StatusCode);

            Assert.Contains(
                "Laboratorio Administrativo",
                html);

            Assert.DoesNotContain(
                "crearReserva=1",
                html);
        }
    }
}