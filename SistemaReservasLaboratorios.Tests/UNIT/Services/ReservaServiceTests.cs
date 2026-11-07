
using System;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.UNIT.Services
{
    public class ReservaServiceTests
    {
        private static AppDbContext CrearContexto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(opciones);
        }

        private static Laboratorio CrearLaboratorio(
            AppDbContext contexto,
            string estado = "Habilitado")
        {
            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio de Pruebas",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = estado
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio;
        }

        private static DateOnly FechaFutura()
        {
            return DateOnly.FromDateTime(
                DateTime.Today.AddDays(7));
        }

        private static void AgregarReserva(
            AppDbContext contexto,
            int laboratorioId,
            DateOnly fecha,
            int horaInicio,
            int horaFin,
            string estado = "Activa")
        {
            contexto.Reservas.Add(new Reserva
            {
                LaboratorioId = laboratorioId,
                Fecha = fecha,
                HoraInicio = new TimeOnly(horaInicio, 0),
                HoraFin = new TimeOnly(horaFin, 0),
                Responsable = "usuario_prueba",
                Estado = estado
            });

            contexto.SaveChanges();
        }

        [Fact]
        public void ConsultarDisponibilidad_SinReservas_EstaDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                laboratorio.Id,
                FechaFutura(),
                new TimeOnly(9, 0),
                new TimeOnly(11, 0));

            // Assert
            Assert.True(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_ReservaActiva_NoDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var fecha = FechaFutura();

            AgregarReserva(
                contexto, laboratorio.Id, fecha, 9, 11);

            var servicio = new ReservaService(contexto);

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                laboratorio.Id,
                fecha,
                new TimeOnly(10, 0),
                new TimeOnly(12, 0));

            // Assert
            Assert.False(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_ReservaCancelada_EstaDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var fecha = FechaFutura();

            AgregarReserva(
                contexto,
                laboratorio.Id,
                fecha,
                9,
                11,
                "Cancelada");

            var servicio = new ReservaService(contexto);

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                laboratorio.Id,
                fecha,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0));

            // Assert
            Assert.True(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_OtroLaboratorio_EstaDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();

            var laboratorioA = CrearLaboratorio(contexto);
            var laboratorioB = CrearLaboratorio(contexto);

            var fecha = FechaFutura();

            AgregarReserva(
                contexto, laboratorioA.Id, fecha, 9, 11);

            var servicio = new ReservaService(contexto);

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                laboratorioB.Id,
                fecha,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0));

            // Assert
            Assert.True(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_OtraFecha_EstaDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var fecha = FechaFutura();

            AgregarReserva(
                contexto, laboratorio.Id, fecha, 9, 11);

            var servicio = new ReservaService(contexto);

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                laboratorio.Id,
                fecha.AddDays(1),
                new TimeOnly(9, 0),
                new TimeOnly(11, 0));

            // Assert
            Assert.True(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_HorariosConsecutivos_EstaDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var fecha = FechaFutura();

            AgregarReserva(
                contexto, laboratorio.Id, fecha, 9, 11);

            var servicio = new ReservaService(contexto);

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                laboratorio.Id,
                fecha,
                new TimeOnly(11, 0),
                new TimeOnly(12, 0));

            // Assert
            Assert.True(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_LaboratorioInexistente_NoDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();
            var servicio = new ReservaService(contexto);

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                99999,
                FechaFutura(),
                new TimeOnly(9, 0),
                new TimeOnly(11, 0));

            // Assert
            Assert.False(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_LaboratorioFueraServicio_NoDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();

            var laboratorio = CrearLaboratorio(
                contexto, "Fuera de servicio");

            var servicio = new ReservaService(contexto);

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                laboratorio.Id,
                FechaFutura(),
                new TimeOnly(9, 0),
                new TimeOnly(11, 0));

            // Assert
            Assert.False(resultado.Disponible);
        }

        [Fact]
        public void ConsultarDisponibilidad_FechaPasada_NoDisponible()
        {
            // Arrange
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var ayer = DateOnly.FromDateTime(
                DateTime.Today.AddDays(-1));

            // Act
            var resultado = servicio.ConsultarDisponibilidad(
                laboratorio.Id,
                ayer,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0));

            // Assert
            Assert.False(resultado.Disponible);
        }
    }
}
