
using System;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.UNIT.Services
{
    public class ValidacionHorarioServiceTests
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
                Nombre = "Laboratorio HU5",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio.Id;
        }

        private static DateOnly FechaFutura()
        {
            return DateOnly.FromDateTime(
                DateTime.Today.AddDays(7));
        }

        [Theory]
        [InlineData(8, 0, 9, 0)]
        [InlineData(18, 0, 22, 0)]
        [InlineData(9, 30, 10, 0)]
        [InlineData(8, 0, 12, 0)]
        public void ConsultarDisponibilidad_HorariosValidos_EstaDisponible(
            int horaInicio, int minutoInicio,
            int horaFin, int minutoFin)
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var resultado = servicio.ConsultarDisponibilidad(
                laboratorioId,
                FechaFutura(),
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFin, minutoFin));

            Assert.True(resultado.Disponible);
        }

        [Theory]
        [InlineData(7, 30, 9, 0)]
        [InlineData(21, 0, 22, 30)]
        public void ConsultarDisponibilidad_FueraHorario_NoDisponible(
            int horaInicio, int minutoInicio,
            int horaFin, int minutoFin)
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var resultado = servicio.ConsultarDisponibilidad(
                laboratorioId,
                FechaFutura(),
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFin, minutoFin));

            Assert.False(resultado.Disponible);
        }

        [Theory]
        [InlineData(9, 15, 10, 0)]
        [InlineData(9, 0, 10, 45)]
        public void ConsultarDisponibilidad_MinutosInvalidos_NoDisponible(
            int horaInicio, int minutoInicio,
            int horaFin, int minutoFin)
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var resultado = servicio.ConsultarDisponibilidad(
                laboratorioId,
                FechaFutura(),
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFin, minutoFin));

            Assert.False(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_DuracionMayorCuatroHoras_NoDisponible()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var resultado = servicio.ConsultarDisponibilidad(
                laboratorioId,
                FechaFutura(),
                new TimeOnly(9, 0),
                new TimeOnly(13, 30));

            Assert.False(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_HorasIguales_NoDisponible()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var resultado = servicio.ConsultarDisponibilidad(
                laboratorioId,
                FechaFutura(),
                new TimeOnly(10, 0),
                new TimeOnly(10, 0));

            Assert.False(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_HoraFinalAnterior_NoDisponible()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var resultado = servicio.ConsultarDisponibilidad(
                laboratorioId,
                FechaFutura(),
                new TimeOnly(12, 0),
                new TimeOnly(10, 0));

            Assert.False(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_FechaActual_EstaDisponible()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var resultado = servicio.ConsultarDisponibilidad(
                laboratorioId,
                hoy,
                new TimeOnly(9, 0),
                new TimeOnly(10, 0));

            Assert.True(resultado.Disponible);
        }

        [Fact]
        public void RegistrarReserva_DuracionInvalida_NoGuarda()
        {
            using var contexto = CrearContexto();
            var laboratorioId = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var resultado = servicio.RegistrarReserva(
                laboratorioId,
                FechaFutura(),
                new TimeOnly(9, 0),
                new TimeOnly(14, 0),
                "usuario_prueba");

            Assert.False(resultado.Disponible);
            Assert.Empty(contexto.Reservas);
        }
    }
}
