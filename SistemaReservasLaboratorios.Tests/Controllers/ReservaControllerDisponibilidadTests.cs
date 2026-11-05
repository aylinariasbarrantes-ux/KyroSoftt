
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SistemaReservasLaboratorios.Controllers;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Controllers
{
    public class ReservaControllerDisponibilidadTests
    {
        private static AppDbContext CrearContexto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(opciones);
        }

        private static ReservaController CrearControlador(
            AppDbContext contexto)
        {
            var reservaService = new ReservaService(contexto);
            var laboratorioService = new LaboratorioService(contexto);

            return new ReservaController(
                reservaService,
                laboratorioService);
        }

        private static int CrearLaboratorio(AppDbContext contexto)
        {
            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio HU3",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio.Id;
        }

        private static string FechaFutura()
        {
            return DateOnly.FromDateTime(
                DateTime.Today.AddDays(7))
                .ToString("yyyy-MM-dd");
        }

        [Fact]
        public void ConsultarDisponibilidad_DatosValidos_RetornaOk()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var controlador = CrearControlador(contexto);

            var resultado = controlador.ConsultarDisponibilidad(
                laboratorioId,
                FechaFutura(),
                "09:00",
                "11:00");

            var ok = Assert.IsType<OkObjectResult>(resultado);
            var disponibilidad =
                Assert.IsType<DisponibilidadResultado>(ok.Value);

            Assert.True(disponibilidad.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_LaboratorioInvalido_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.ConsultarDisponibilidad(
                0,
                FechaFutura(),
                "09:00",
                "11:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void ConsultarDisponibilidad_FechaInvalida_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.ConsultarDisponibilidad(
                1,
                "fecha-invalida",
                "09:00",
                "11:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void ConsultarDisponibilidad_HoraInicioInvalida_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.ConsultarDisponibilidad(
                1,
                FechaFutura(),
                "hora-invalida",
                "11:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void ConsultarDisponibilidad_HoraFinInvalida_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.ConsultarDisponibilidad(
                1,
                FechaFutura(),
                "09:00",
                "hora-invalida");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void ConsultarDisponibilidad_IntervaloInvalido_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.ConsultarDisponibilidad(
                1,
                FechaFutura(),
                "09:15",
                "11:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void ConsultarDisponibilidad_HoraFinalAnterior_RetornaBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.ConsultarDisponibilidad(
                1,
                FechaFutura(),
                "12:00",
                "10:00");

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public void ConsultarDisponibilidad_HorarioOcupado_RetornaOkNoDisponible()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var fecha = DateOnly.FromDateTime(
                DateTime.Today.AddDays(7));

            contexto.Reservas.Add(new Reserva
            {
                LaboratorioId = laboratorioId,
                Fecha = fecha,
                HoraInicio = new TimeOnly(9, 0),
                HoraFin = new TimeOnly(11, 0),
                Responsable = "usuario_prueba",
                Estado = "Activa"
            });

            contexto.SaveChanges();

            var controlador = CrearControlador(contexto);

            var resultado = controlador.ConsultarDisponibilidad(
                laboratorioId,
                fecha.ToString("yyyy-MM-dd"),
                "10:00",
                "12:00");

            var ok = Assert.IsType<OkObjectResult>(resultado);
            var disponibilidad =
                Assert.IsType<DisponibilidadResultado>(ok.Value);

            Assert.False(disponibilidad.Disponible);
        }
    }
}
