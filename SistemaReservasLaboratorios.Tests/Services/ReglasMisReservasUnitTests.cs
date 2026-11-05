
using System;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Services
{
    public class ReglasMisReservasUnitTests
    {
        private static readonly DateTime Ahora =
            new DateTime(2026, 11, 4, 12, 0, 0);

        [Fact]
        public void EstadoVisible_ReservaActivaFutura_EsActiva()
        {
            var resultado = ReglasMisReservas.ObtenerEstadoVisible(
                "Activa",
                new DateOnly(2026, 11, 5),
                new TimeOnly(10, 0),
                Ahora);

            Assert.Equal("Activa", resultado);
        }

        [Fact]
        public void EstadoVisible_ReservaActivaPasada_EsExpirada()
        {
            var resultado = ReglasMisReservas.ObtenerEstadoVisible(
                "Activa",
                new DateOnly(2026, 11, 3),
                new TimeOnly(10, 0),
                Ahora);

            Assert.Equal("Expirada", resultado);
        }

        [Fact]
        public void EstadoVisible_ReservaCancelada_ConservaEstado()
        {
            var resultado = ReglasMisReservas.ObtenerEstadoVisible(
                "Cancelada",
                new DateOnly(2026, 11, 3),
                new TimeOnly(10, 0),
                Ahora);

            Assert.Equal("Cancelada", resultado);
        }

        [Fact]
        public void EstadoVisible_ReservaActivaTerminadaHoy_EsExpirada()
        {
            var resultado = ReglasMisReservas.ObtenerEstadoVisible(
                "Activa",
                new DateOnly(2026, 11, 4),
                new TimeOnly(11, 0),
                Ahora);

            Assert.Equal("Expirada", resultado);
        }

        [Fact]
        public void EstadoVisible_ReservaActivaTerminaExactamenteAhora_EsExpirada()
        {
            var resultado = ReglasMisReservas.ObtenerEstadoVisible(
                "Activa",
                new DateOnly(2026, 11, 4),
                new TimeOnly(12, 0),
                Ahora);

            Assert.Equal("Expirada", resultado);
        }

        [Fact]
        public void PuedeMostrarCancelacion_ActivaFutura_DevuelveTrue()
        {
            var resultado = ReglasMisReservas.PuedeMostrarCancelacion(
                "Activa",
                new DateOnly(2026, 11, 5),
                new TimeOnly(10, 0),
                Ahora);

            Assert.True(resultado);
        }

        [Fact]
        public void PuedeMostrarCancelacion_ActivaHoyNoTerminada_DevuelveTrue()
        {
            var resultado = ReglasMisReservas.PuedeMostrarCancelacion(
                "Activa",
                new DateOnly(2026, 11, 4),
                new TimeOnly(15, 0),
                Ahora);

            Assert.True(resultado);
        }

        [Fact]
        public void PuedeMostrarCancelacion_Expirada_DevuelveFalse()
        {
            var resultado = ReglasMisReservas.PuedeMostrarCancelacion(
                "Activa",
                new DateOnly(2026, 11, 3),
                new TimeOnly(10, 0),
                Ahora);

            Assert.False(resultado);
        }

        [Fact]
        public void PuedeMostrarCancelacion_Cancelada_DevuelveFalse()
        {
            var resultado = ReglasMisReservas.PuedeMostrarCancelacion(
                "Cancelada",
                new DateOnly(2026, 11, 5),
                new TimeOnly(10, 0),
                Ahora);

            Assert.False(resultado);
        }

        [Fact]
        public void PuedeMostrarCancelacion_TerminaExactamenteAhora_DevuelveFalse()
        {
            var resultado = ReglasMisReservas.PuedeMostrarCancelacion(
                "Activa",
                new DateOnly(2026, 11, 4),
                new TimeOnly(12, 0),
                Ahora);

            Assert.False(resultado);
        }
    }
}
