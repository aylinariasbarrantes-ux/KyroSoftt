
using System;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Services
{
    public class ReglasReservaUnitTests
    {
        private static readonly DateOnly Hoy =
            new DateOnly(2026, 11, 4);

        // HU5: La hora final debe ser posterior a la inicial.

        [Fact]
        public void ValidarHorario_HoraFinalAnterior_Rechaza()
        {
            var resultado = ReglasReserva.ValidarHorario(
                Hoy,
                new TimeOnly(12, 0),
                new TimeOnly(10, 0),
                Hoy);

            Assert.NotNull(resultado);
            Assert.Contains("posterior", resultado);
        }

        [Fact]
        public void ValidarHorario_HorasIguales_Rechaza()
        {
            var resultado = ReglasReserva.ValidarHorario(
                Hoy,
                new TimeOnly(10, 0),
                new TimeOnly(10, 0),
                Hoy);

            Assert.NotNull(resultado);
        }

        // HU5: No se permiten fechas anteriores.

        [Fact]
        public void ValidarHorario_FechaPasada_Rechaza()
        {
            var resultado = ReglasReserva.ValidarHorario(
                Hoy.AddDays(-1),
                new TimeOnly(9, 0),
                new TimeOnly(10, 0),
                Hoy);

            Assert.Contains("pasado", resultado!);
        }

        // HU5: Horario permitido entre 08:00 y 22:00.

        [Theory]
        [InlineData(7, 30, 9, 0)]
        [InlineData(21, 0, 22, 30)]
        public void ValidarHorario_FueraDeHorario_Rechaza(
            int horaInicio,
            int minutoInicio,
            int horaFin,
            int minutoFin)
        {
            var resultado = ReglasReserva.ValidarHorario(
                Hoy,
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFin, minutoFin),
                Hoy);

            Assert.Contains("horario permitido", resultado!);
        }

        // HU5: Duración máxima de cuatro horas.

        [Fact]
        public void ValidarHorario_MasDeCuatroHoras_Rechaza()
        {
            var resultado = ReglasReserva.ValidarHorario(
                Hoy,
                new TimeOnly(8, 0),
                new TimeOnly(12, 30),
                Hoy);

            Assert.Contains("4 horas", resultado!);
        }

        [Fact]
        public void ValidarHorario_ExactamenteCuatroHoras_Acepta()
        {
            var resultado = ReglasReserva.ValidarHorario(
                Hoy,
                new TimeOnly(8, 0),
                new TimeOnly(12, 0),
                Hoy);

            Assert.Null(resultado);
        }

        // HU5: Intervalos de treinta minutos.

        [Theory]
        [InlineData(9, 15, 10, 0)]
        [InlineData(9, 0, 10, 45)]
        public void ValidarHorario_MinutosInvalidos_Rechaza(
            int horaInicio,
            int minutoInicio,
            int horaFin,
            int minutoFin)
        {
            var resultado = ReglasReserva.ValidarHorario(
                Hoy,
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFin, minutoFin),
                Hoy);

            Assert.Contains("30 minutos", resultado!);
        }

        [Fact]
        public void ValidarHorario_HorarioValido_Acepta()
        {
            var resultado = ReglasReserva.ValidarHorario(
                Hoy.AddDays(7),
                new TimeOnly(9, 0),
                new TimeOnly(11, 30),
                Hoy);

            Assert.Null(resultado);
        }

        // HU6: Detectar conflictos entre horarios.

        [Theory]
        [InlineData(9, 0, 11, 0, 10, 0, 12, 0)]
        [InlineData(9, 0, 11, 0, 8, 0, 10, 0)]
        [InlineData(9, 0, 11, 0, 9, 0, 11, 0)]
        [InlineData(9, 0, 11, 0, 8, 0, 12, 0)]
        [InlineData(9, 0, 11, 0, 9, 30, 10, 30)]
        public void HayTraslape_HorariosSuperpuestos_DevuelveTrue(
            int hi1, int mi1,
            int hf1, int mf1,
            int hi2, int mi2,
            int hf2, int mf2)
        {
            var resultado = ReglasReserva.HayTraslape(
                new TimeOnly(hi1, mi1),
                new TimeOnly(hf1, mf1),
                new TimeOnly(hi2, mi2),
                new TimeOnly(hf2, mf2));

            Assert.True(resultado);
        }

        // HU6: Los horarios consecutivos no se traslapan.

        [Theory]
        [InlineData(9, 0, 11, 0, 11, 0, 12, 0)]
        [InlineData(9, 0, 11, 0, 8, 0, 9, 0)]
        public void HayTraslape_HorariosConsecutivos_DevuelveFalse(
            int hi1, int mi1,
            int hf1, int mf1,
            int hi2, int mi2,
            int hf2, int mf2)
        {
            var resultado = ReglasReserva.HayTraslape(
                new TimeOnly(hi1, mi1),
                new TimeOnly(hf1, mf1),
                new TimeOnly(hi2, mi2),
                new TimeOnly(hf2, mf2));

            Assert.False(resultado);
        }

        [Fact]
        public void HayTraslape_HorariosSeparados_DevuelveFalse()
        {
            var resultado = ReglasReserva.HayTraslape(
                new TimeOnly(8, 0),
                new TimeOnly(10, 0),
                new TimeOnly(14, 0),
                new TimeOnly(16, 0));

            Assert.False(resultado);
        }
    }
}
