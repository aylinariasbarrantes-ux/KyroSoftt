
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.UNIT.Services
{
    public class MisReservasServiceTests
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
            string nombre = "Laboratorio HU7")
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

        private static Reserva AgregarReserva(
            AppDbContext contexto,
            int laboratorioId,
            string responsable,
            int diasDesdeHoy,
            int horaInicio,
            string estado = "Activa")
        {
            var reserva = new Reserva
            {
                LaboratorioId = laboratorioId,
                Responsable = responsable,
                Fecha = DateOnly.FromDateTime(
                    DateTime.Today.AddDays(diasDesdeHoy)),
                HoraInicio = new TimeOnly(horaInicio, 0),
                HoraFin = new TimeOnly(horaInicio + 1, 0),
                Estado = estado
            };

            contexto.Reservas.Add(reserva);
            contexto.SaveChanges();

            return reserva;
        }

        // HU7-01: El usuario puede consultar sus reservas.

        [Fact]
        public void ObtenerMisReservas_UsuarioConReservas_DevuelveSusReservas()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", 5, 9);
            AgregarReserva(contexto, laboratorio.Id, "aylin", 6, 10);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            Assert.Equal(2, resultado.Count);
            Assert.All(resultado,
                reserva => Assert.Equal("aylin", reserva.Responsable));
        }

        // HU7-02: No deben mostrarse reservas de otros usuarios.

        [Fact]
        public void ObtenerMisReservas_OtrosUsuarios_NoDevuelveReservasAjenas()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", 5, 9);
            AgregarReserva(contexto, laboratorio.Id, "maria", 6, 10);
            AgregarReserva(contexto, laboratorio.Id, "carlos", 7, 11);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            var reserva = Assert.Single(resultado);
            Assert.Equal("aylin", reserva.Responsable);
        }

        // HU7-03: Un usuario sin reservas obtiene una lista vacía.

        [Fact]
        public void ObtenerMisReservas_UsuarioSinReservas_DevuelveListaVacia()
        {
            using var contexto = CrearContexto();

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            Assert.NotNull(resultado);
            Assert.Empty(resultado);
        }

        // HU7-04: No devuelve reservas ajenas si el usuario no tiene ninguna.

        [Fact]
        public void ObtenerMisReservas_SoloExistenReservasAjenas_DevuelveListaVacia()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "maria", 5, 9);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            Assert.Empty(resultado);
        }

        // HU7-05: Ordena las fechas de la más reciente a la más antigua.

        [Fact]
        public void ObtenerMisReservas_VariasFechas_OrdenaDescendentemente()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", 3, 9);
            AgregarReserva(contexto, laboratorio.Id, "aylin", 10, 10);
            AgregarReserva(contexto, laboratorio.Id, "aylin", 6, 11);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            Assert.Equal(3, resultado.Count);
            Assert.True(resultado[0].Fecha > resultado[1].Fecha);
            Assert.True(resultado[1].Fecha > resultado[2].Fecha);
        }

        // HU7-06: Para una misma fecha, ordena por hora de inicio.

        [Fact]
        public void ObtenerMisReservas_MismaFecha_OrdenaPorHoraAscendente()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", 5, 14);
            AgregarReserva(contexto, laboratorio.Id, "aylin", 5, 8);
            AgregarReserva(contexto, laboratorio.Id, "aylin", 5, 11);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            Assert.Equal(3, resultado.Count);
            Assert.Equal(new TimeOnly(8, 0), resultado[0].HoraInicio);
            Assert.Equal(new TimeOnly(11, 0), resultado[1].HoraInicio);
            Assert.Equal(new TimeOnly(14, 0), resultado[2].HoraInicio);
        }

        // HU7-07: Incluye la información del laboratorio.

        [Fact]
        public void ObtenerMisReservas_ReservaExistente_IncluyeLaboratorio()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(
                contexto, "Laboratorio de Redes");

            AgregarReserva(contexto, laboratorio.Id, "aylin", 5, 9);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            var reserva = Assert.Single(resultado);

            Assert.NotNull(reserva.Laboratorio);
            Assert.Equal(
                "Laboratorio de Redes",
                reserva.Laboratorio!.Nombre);
        }

        // HU7-08: Las reservas canceladas permanecen en el historial.

        [Fact]
        public void ObtenerMisReservas_ReservaCancelada_ApareceEnHistorial()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(
                contexto,
                laboratorio.Id,
                "aylin",
                5,
                9,
                "Cancelada");

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            var reserva = Assert.Single(resultado);

            Assert.Equal("Cancelada", reserva.Estado);
        }

        // HU7-09: También muestra reservas de fechas anteriores.

        [Fact]
        public void ObtenerMisReservas_ReservaPasada_ApareceEnHistorial()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", -5, 9);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.ObtenerMisReservas("aylin");

            var reserva = Assert.Single(resultado);

            Assert.True(
                reserva.Fecha <
                DateOnly.FromDateTime(DateTime.Today));
        }

        // HU7-10: Consultar reservas no debe modificar la base de datos.

        [Fact]
        public void ObtenerMisReservas_Consulta_NoModificaRegistros()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", 5, 9);

            var servicio = new ReservaService(contexto);

            var cantidadAntes = contexto.Reservas.Count();

            var resultado = servicio.ObtenerMisReservas("aylin");

            var cantidadDespues = contexto.Reservas.Count();

            Assert.Single(resultado);
            Assert.Equal(cantidadAntes, cantidadDespues);
        }
    }
}
