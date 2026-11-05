
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Services
{
    public class ReglasFormularioReservaUnitTests
    {
        private static string? Validar(
            int laboratorioId,
            string? fecha,
            string? inicio,
            string? fin)
        {
            var resultado = ReglasFormularioReserva.ParsearHorario(
                laboratorioId,
                fecha,
                inicio,
                fin,
                out _,
                out _,
                out _);

            return resultado?.Motivo;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void LaboratorioInvalido_Rechaza(int id)
        {
            var resultado = Validar(
                id, "2026-12-10", "09:00", "10:00");

            Assert.Contains("laboratorio válido", resultado!);
        }

        [Theory]
        [InlineData("")]
        [InlineData("10/12/2026")]
        [InlineData("2026-13-10")]
        [InlineData("fecha incorrecta")]
        public void FechaInvalida_Rechaza(string fecha)
        {
            var resultado = Validar(
                1, fecha, "09:00", "10:00");

            Assert.Contains("fecha no es válida", resultado!);
        }

        [Theory]
        [InlineData("")]
        [InlineData("9 AM")]
        [InlineData("25:00")]
        public void HoraInicioInvalida_Rechaza(string inicio)
        {
            var resultado = Validar(
                1, "2026-12-10", inicio, "10:00");

            Assert.Contains("hora de inicio", resultado!);
        }

        [Theory]
        [InlineData("")]
        [InlineData("10 PM")]
        [InlineData("25:00")]
        public void HoraFinInvalida_Rechaza(string fin)
        {
            var resultado = Validar(
                1, "2026-12-10", "09:00", fin);

            Assert.Contains("hora de fin", resultado!);
        }

        [Theory]
        [InlineData("09:15", "10:00")]
        [InlineData("09:00", "10:45")]
        public void IntervaloInvalido_Rechaza(
            string inicio,
            string fin)
        {
            var resultado = Validar(
                1, "2026-12-10", inicio, fin);

            Assert.Contains("30 minutos", resultado!);
        }

        [Theory]
        [InlineData("10:00", "09:00")]
        [InlineData("10:00", "10:00")]
        public void HorarioInvertido_Rechaza(
            string inicio,
            string fin)
        {
            var resultado = Validar(
                1, "2026-12-10", inicio, fin);

            Assert.Contains("posterior", resultado!);
        }

        [Fact]
        public void DatosValidos_Acepta()
        {
            var resultado = Validar(
                1, "2026-12-10", "09:00", "10:30");

            Assert.Null(resultado);
        }

        [Fact]
        public void DatosValidos_ConvierteCorrectamente()
        {
            var resultado = ReglasFormularioReserva.ParsearHorario(
                1,
                "2026-12-10",
                "09:00",
                "10:30",
                out var fecha,
                out var inicio,
                out var fin);

            Assert.Null(resultado);
            Assert.Equal(new DateOnly(2026, 12, 10), fecha);
            Assert.Equal(new TimeOnly(9, 0), inicio);
            Assert.Equal(new TimeOnly(10, 30), fin);
        }
    }
}
