
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.UNIT.Services
{
    public class ReservaTraslapeServiceTests
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
            string nombre)
        {
            var laboratorio = new Laboratorio
            {
                Nombre = nombre,
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
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
            string estado = "Activa")
        {
            contexto.Reservas.Add(new Reserva
            {
                LaboratorioId = laboratorioId,
                Fecha = fecha,
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(12, 0),
                Responsable = "usuario_original",
                Estado = estado
            });

            contexto.SaveChanges();
        }

        // Reserva existente: 10:00 a 12:00.
        // Todos estos horarios se traslapan.

        [Theory]
        [InlineData(10, 0, 12, 0)]
        [InlineData(9, 0, 11, 0)]
        [InlineData(11, 0, 13, 0)]
        [InlineData(9, 0, 13, 0)]
        [InlineData(10, 30, 11, 30)]
        [InlineData(9, 30, 10, 30)]
        [InlineData(11, 30, 12, 30)]
        public void RegistrarReserva_ConTraslape_RechazaRegistro(
            int horaInicio,
            int minutoInicio,
            int horaFin,
            int minutoFin)
        {
            using var contexto = CrearContexto();

            var laboratorio = CrearLaboratorio(
                contexto, "Laboratorio HU6");

            var fecha = FechaFutura();

            AgregarReserva(contexto, laboratorio.Id, fecha);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha,
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFin, minutoFin),
                "usuario_nuevo");

            Assert.False(resultado.Disponible);
            Assert.Single(contexto.Reservas);
        }

        [Theory]
        [InlineData(8, 0, 10, 0)]
        [InlineData(12, 0, 14, 0)]
        public void RegistrarReserva_SinTraslape_PermiteRegistro(
            int horaInicio,
            int minutoInicio,
            int horaFin,
            int minutoFin)
        {
            using var contexto = CrearContexto();

            var laboratorio = CrearLaboratorio(
                contexto, "Laboratorio HU6");

            var fecha = FechaFutura();

            AgregarReserva(contexto, laboratorio.Id, fecha);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha,
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFin, minutoFin),
                "usuario_nuevo");

            Assert.True(resultado.Disponible);
            Assert.Equal(2, contexto.Reservas.Count());
        }

        [Fact]
        public void RegistrarReserva_OtroLaboratorio_PermiteMismoHorario()
        {
            using var contexto = CrearContexto();

            var laboratorioUno = CrearLaboratorio(
                contexto, "Laboratorio A");

            var laboratorioDos = CrearLaboratorio(
                contexto, "Laboratorio B");

            var fecha = FechaFutura();

            AgregarReserva(contexto, laboratorioUno.Id, fecha);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.RegistrarReserva(
                laboratorioDos.Id,
                fecha,
                new TimeOnly(10, 0),
                new TimeOnly(12, 0),
                "usuario_nuevo");

            Assert.True(resultado.Disponible);
            Assert.Equal(2, contexto.Reservas.Count());
        }

        [Fact]
        public void RegistrarReserva_OtraFecha_PermiteMismoHorario()
        {
            using var contexto = CrearContexto();

            var laboratorio = CrearLaboratorio(
                contexto, "Laboratorio HU6");

            var fecha = FechaFutura();

            AgregarReserva(contexto, laboratorio.Id, fecha);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha.AddDays(1),
                new TimeOnly(10, 0),
                new TimeOnly(12, 0),
                "usuario_nuevo");

            Assert.True(resultado.Disponible);
            Assert.Equal(2, contexto.Reservas.Count());
        }

        [Fact]
        public void RegistrarReserva_ReservaCancelada_PermiteReutilizarHorario()
        {
            using var contexto = CrearContexto();

            var laboratorio = CrearLaboratorio(
                contexto, "Laboratorio HU6");

            var fecha = FechaFutura();

            AgregarReserva(
                contexto,
                laboratorio.Id,
                fecha,
                "Cancelada");

            var servicio = new ReservaService(contexto);

            var resultado = servicio.RegistrarReserva(
                laboratorio.Id,
                fecha,
                new TimeOnly(10, 0),
                new TimeOnly(12, 0),
                "usuario_nuevo");

            Assert.True(resultado.Disponible);
            Assert.Equal(2, contexto.Reservas.Count());

            Assert.Single(
                contexto.Reservas.Where(r => r.Estado == "Activa"));
        }
    }
}
