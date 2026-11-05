
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Controllers;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using SistemaReservasLaboratorios.Tests.Helpers;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Controllers
{
    public class ReservaControllerRegistroTests
    {
        private static AppDbContext CrearContexto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(opciones);
        }

        private static int CrearLaboratorio(AppDbContext contexto)
        {
            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio HU4",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio.Id;
        }

        private static ReservaController CrearControlador(
            AppDbContext contexto,
            string? usuario = "usuario_prueba")
        {
            var controlador = new ReservaController(
                new ReservaService(contexto),
                new LaboratorioService(contexto));

            var httpContext = new DefaultHttpContext();
            var sesion = new FakeSession();

            if (usuario != null)
            {
                sesion.SetString("NombreUsuario", usuario);
                sesion.SetString("Rol", "Usuario");
            }

            httpContext.Session = sesion;

            controlador.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controlador;
        }

        private static string FechaFutura()
        {
            return DateOnly.FromDateTime(
                DateTime.Today.AddDays(7))
                .ToString("yyyy-MM-dd");
        }

        [Fact]
        public void RegistrarReserva_DatosValidos_RetornaOkYGuarda()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var controlador = CrearControlador(contexto);

            var resultado = controlador.RegistrarReserva(
                laboratorioId,
                FechaFutura(),
                "09:00",
                "11:00");

            var ok = Assert.IsType<OkObjectResult>(resultado);
            var disponibilidad =
                Assert.IsType<DisponibilidadResultado>(ok.Value);

            Assert.True(disponibilidad.Disponible);

            var reserva = Assert.Single(contexto.Reservas);
            Assert.Equal("usuario_prueba", reserva.Responsable);
            Assert.Equal("Activa", reserva.Estado);
        }

        [Fact]
        public void RegistrarReserva_SinUsuario_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var controlador = CrearControlador(contexto, null);

            var resultado = controlador.RegistrarReserva(
                laboratorioId,
                FechaFutura(),
                "09:00",
                "11:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Empty(contexto.Reservas);
        }

        [Fact]
        public void RegistrarReserva_LaboratorioInvalido_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.RegistrarReserva(
                0,
                FechaFutura(),
                "09:00",
                "11:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void RegistrarReserva_FechaInvalida_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.RegistrarReserva(
                1,
                "fecha-invalida",
                "09:00",
                "11:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void RegistrarReserva_HoraInvalida_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.RegistrarReserva(
                1,
                FechaFutura(),
                "09:15",
                "11:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void RegistrarReserva_HorarioOcupado_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var controlador = CrearControlador(contexto);

            var primera = controlador.RegistrarReserva(
                laboratorioId,
                FechaFutura(),
                "09:00",
                "11:00");

            var segunda = controlador.RegistrarReserva(
                laboratorioId,
                FechaFutura(),
                "10:00",
                "12:00");

            Assert.IsType<OkObjectResult>(primera);
            Assert.IsType<BadRequestObjectResult>(segunda);
            Assert.Single(contexto.Reservas);
        }
    }
}
