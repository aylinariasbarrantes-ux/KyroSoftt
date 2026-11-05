
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Services
{
    public class RegistrarReservaServiceTests
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
                Nombre = "Laboratorio HU4",
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

        [Fact]
        public void RegistrarReserva_DatosValidos_GuardaReservaActiva()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);
            var fecha = FechaFutura();

            var resultado = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0),
                "usuario_prueba");

            Assert.True(resultado.Disponible);

            var reserva = Assert.Single(contexto.Reservas);

            Assert.Equal(laboratorio.Id, reserva.LaboratorioId);
            Assert.Equal(fecha, reserva.Fecha);
            Assert.Equal(new TimeOnly(9, 0), reserva.HoraInicio);
            Assert.Equal(new TimeOnly(11, 0), reserva.HoraFin);
            Assert.Equal("usuario_prueba", reserva.Responsable);
            Assert.Equal("Activa", reserva.Estado);
        }

        [Fact]
        public void RegistrarReserva_ResponsableVacio_NoGuarda()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);

            var resultado = servicio.RegistrarReserva(
                laboratorio.Id,
                FechaFutura(),
                new TimeOnly(9, 0),
                new TimeOnly(11, 0),
                " ");

            Assert.False(resultado.Disponible);
            Assert.Empty(contexto.Reservas);
        }

        [Fact]
        public void RegistrarReserva_HorarioOcupado_NoDuplicaReserva()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);
            var fecha = FechaFutura();

            var primera = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0),
                "usuario_uno");

            var segunda = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha,
                new TimeOnly(10, 0),
                new TimeOnly(12, 0),
                "usuario_dos");

            Assert.True(primera.Disponible);
            Assert.False(segunda.Disponible);
            Assert.Single(contexto.Reservas);
        }

        [Fact]
        public void RegistrarReserva_LaboratorioFueraServicio_NoGuarda()
        {
            using var contexto = CrearContexto();

            var laboratorio = CrearLaboratorio(
                contexto, "Fuera de servicio");

            var servicio = new ReservaService(contexto);

            var resultado = servicio.RegistrarReserva(
                laboratorio.Id,
                FechaFutura(),
                new TimeOnly(9, 0),
                new TimeOnly(11, 0),
                "usuario_prueba");

            Assert.False(resultado.Disponible);
            Assert.Empty(contexto.Reservas);
        }

        [Fact]
        public void RegistrarReserva_ReservasConsecutivas_GuardaAmbas()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var servicio = new ReservaService(contexto);
            var fecha = FechaFutura();

            var primera = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0),
                "usuario_uno");

            var segunda = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha,
                new TimeOnly(11, 0),
                new TimeOnly(12, 0),
                "usuario_dos");

            Assert.True(primera.Disponible);
            Assert.True(segunda.Disponible);
            Assert.Equal(2, contexto.Reservas.Count());
        }
    }
}
