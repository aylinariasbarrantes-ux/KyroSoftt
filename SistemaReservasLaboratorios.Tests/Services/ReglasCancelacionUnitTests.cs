
using System;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Services
{
    public class ReglasCancelacionUnitTests
    {
        [Fact]
        public void PuedeCancelar_Propietario_DevuelveTrue()
        {
            Assert.True(ReglasCancelacion.PuedeCancelar(
                "aylin", "aylin", false));
        }

        [Fact]
        public void PuedeCancelar_OtroUsuario_DevuelveFalse()
        {
            Assert.False(ReglasCancelacion.PuedeCancelar(
                "maria", "aylin", false));
        }

        [Fact]
        public void PuedeCancelar_Administrador_DevuelveTrue()
        {
            Assert.True(ReglasCancelacion.PuedeCancelar(
                "maria", "admin", true));
        }

        [Fact]
        public void PuedeCancelar_MayusculasDiferentes_DevuelveTrue()
        {
            Assert.True(ReglasCancelacion.PuedeCancelar(
                "Aylin", "aylin", false));
        }

        [Fact]
        public void PuedeCancelar_AdministradorSinSerPropietario_DevuelveTrue()
        {
            Assert.True(ReglasCancelacion.PuedeCancelar(
                "usuario1", "administrador", true));
        }

        [Fact]
        public void EstaExpirada_FechaPasada_DevuelveTrue()
        {
            var ahora = new DateTime(2026, 11, 4, 12, 0, 0);

            Assert.True(ReglasCancelacion.EstaExpirada(
                new DateOnly(2026, 11, 3),
                new TimeOnly(18, 0),
                ahora));
        }

        [Fact]
        public void EstaExpirada_FechaFutura_DevuelveFalse()
        {
            var ahora = new DateTime(2026, 11, 4, 12, 0, 0);

            Assert.False(ReglasCancelacion.EstaExpirada(
                new DateOnly(2026, 11, 5),
                new TimeOnly(10, 0),
                ahora));
        }

        [Fact]
        public void EstaExpirada_HoyHoraFinalPasada_DevuelveTrue()
        {
            var ahora = new DateTime(2026, 11, 4, 12, 0, 0);

            Assert.True(ReglasCancelacion.EstaExpirada(
                new DateOnly(2026, 11, 4),
                new TimeOnly(11, 0),
                ahora));
        }

        [Fact]
        public void EstaExpirada_HoyHoraFinalFutura_DevuelveFalse()
        {
            var ahora = new DateTime(2026, 11, 4, 12, 0, 0);

            Assert.False(ReglasCancelacion.EstaExpirada(
                new DateOnly(2026, 11, 4),
                new TimeOnly(15, 0),
                ahora));
        }

        [Fact]
        public void EstaExpirada_HoyHoraFinalExacta_DevuelveTrue()
        {
            var ahora = new DateTime(2026, 11, 4, 12, 0, 0);

            Assert.True(ReglasCancelacion.EstaExpirada(
                new DateOnly(2026, 11, 4),
                new TimeOnly(12, 0),
                ahora));
        }
    }
}
