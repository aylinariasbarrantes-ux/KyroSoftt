
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Services
{
    public class CancelarReservaServiceTests
    {
        private static AppDbContext CrearContexto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(opciones);
        }

        private static Laboratorio CrearLaboratorio(
            AppDbContext contexto)
        {
            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio HU8",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio;
        }

        private static Reserva CrearReserva(
            AppDbContext contexto,
            int laboratorioId,
            string responsable = "aylin",
            int diasDesdeHoy = 7,
            string estado = "Activa")
        {
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

            return reserva;
        }

        // HU8-01: El propietario puede cancelar su reserva.

        [Fact]
        public void CancelarReserva_Propietario_CancelaCorrectamente()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto, laboratorio.Id);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            Assert.True(resultado.Exito);
            Assert.Equal(
                "Cancelada",
                contexto.Reservas.Find(reserva.Id)!.Estado);
        }

        // HU8-02: Un usuario no puede cancelar reservas ajenas.

        [Fact]
        public void CancelarReserva_UsuarioNoPropietario_RechazaCancelacion()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto, laboratorio.Id, "maria");

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            Assert.False(resultado.Exito);
            Assert.Equal(
                "Activa",
                contexto.Reservas.Find(reserva.Id)!.Estado);
        }

        // HU8-03: El administrador puede cancelar reservas ajenas.

        [Fact]
        public void CancelarReserva_Administrador_CancelaReservaAjena()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto, laboratorio.Id, "maria");

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                reserva.Id, "admin", true);

            Assert.True(resultado.Exito);
            Assert.Equal(
                "Cancelada",
                contexto.Reservas.Find(reserva.Id)!.Estado);
        }

        // HU8-04: No se puede cancelar una reserva inexistente.

        [Fact]
        public void CancelarReserva_ReservaInexistente_DevuelveError()
        {
            using var contexto = CrearContexto();

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                99999, "aylin", false);

            Assert.False(resultado.Exito);
        }

        // HU8-05: Una reserva cancelada no se cancela otra vez.

        [Fact]
        public void CancelarReserva_YaCancelada_RechazaOperacion()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto,
                laboratorio.Id,
                "aylin",
                7,
                "Cancelada");

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            Assert.False(resultado.Exito);
            Assert.Equal(
                "Cancelada",
                contexto.Reservas.Find(reserva.Id)!.Estado);
        }

        // HU8-06: No se puede cancelar una reserva pasada.

        [Fact]
        public void CancelarReserva_ReservaPasada_RechazaCancelacion()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto,
                laboratorio.Id,
                "aylin",
                -7);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            Assert.False(resultado.Exito);
            Assert.Equal(
                "Activa",
                contexto.Reservas.Find(reserva.Id)!.Estado);
        }

        // HU8-07: La cancelación no elimina el registro.

        [Fact]
        public void CancelarReserva_CancelacionExitosa_ConservaRegistro()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto, laboratorio.Id);

            var servicio = new ReservaService(contexto);

            var cantidadAntes = contexto.Reservas.Count();

            var resultado = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            var cantidadDespues = contexto.Reservas.Count();

            Assert.True(resultado.Exito);
            Assert.Equal(cantidadAntes, cantidadDespues);
            Assert.NotNull(contexto.Reservas.Find(reserva.Id));
        }

        // HU8-08: Una reserva cancelada libera el horario.

        [Fact]
        public void CancelarReserva_CancelacionExitosa_LiberaHorario()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto, laboratorio.Id);

            var servicio = new ReservaService(contexto);

            var disponibilidadAntes =
                servicio.ConsultarDisponibilidad(
                    laboratorio.Id,
                    reserva.Fecha,
                    reserva.HoraInicio,
                    reserva.HoraFin);

            Assert.False(disponibilidadAntes.Disponible);

            var cancelacion = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            Assert.True(cancelacion.Exito);

            var disponibilidadDespues =
                servicio.ConsultarDisponibilidad(
                    laboratorio.Id,
                    reserva.Fecha,
                    reserva.HoraInicio,
                    reserva.HoraFin);

            Assert.True(disponibilidadDespues.Disponible);
        }

        // HU8-09: El propietario se compara sin distinguir mayúsculas.

        [Fact]
        public void CancelarReserva_UsuarioConMayusculas_PermiteCancelar()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto, laboratorio.Id, "Aylin");

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            Assert.True(resultado.Exito);
        }

        // HU8-10: No se puede cancelar dos veces consecutivas.

        [Fact]
        public void CancelarReserva_DobleCancelacion_SegundaFalla()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto, laboratorio.Id);

            var servicio = new ReservaService(contexto);

            var primera = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            var segunda = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            Assert.True(primera.Exito);
            Assert.False(segunda.Exito);
        }

        // HU8-11: Cancelar una reserva no afecta a otra.

        [Fact]
        public void CancelarReserva_UnaReserva_NoModificaOtras()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var primera = CrearReserva(
                contexto, laboratorio.Id, "aylin", 7);

            var segunda = CrearReserva(
                contexto, laboratorio.Id, "maria", 8);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                primera.Id, "aylin", false);

            Assert.True(resultado.Exito);

            Assert.Equal(
                "Cancelada",
                contexto.Reservas.Find(primera.Id)!.Estado);

            Assert.Equal(
                "Activa",
                contexto.Reservas.Find(segunda.Id)!.Estado);
        }

        // HU8-12: Una reserva cancelada deja de aparecer en calendario.

        [Fact]
        public void CancelarReserva_ReservaCancelada_NoApareceEnCalendario()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            var reserva = CrearReserva(
                contexto, laboratorio.Id);

            var servicio = new ReservaService(contexto);

            var resultado = servicio.CancelarReserva(
                reserva.Id, "aylin", false);

            Assert.True(resultado.Exito);

            var calendario =
                servicio.ObtenerReservasCalendario();

            Assert.DoesNotContain(
                calendario,
                r => r.Id == reserva.Id);
        }
    }
}
