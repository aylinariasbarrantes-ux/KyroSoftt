using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.UNIT.Services
{
    public class ReglasLoginUnitTests
    {
        [Theory]
        [InlineData("", "123456")]
        [InlineData("usuario", "")]
        [InlineData(" ", "123456")]
        [InlineData("usuario", " ")]
        [InlineData(null, "123456")]
        [InlineData("usuario", null)]
        public void ValidarCampos_CamposVacios_Rechaza(
            string? usuario,
            string? password)
        {
            var resultado = ReglasLogin.ValidarCampos(
                usuario, password);

            Assert.Equal(
                "Debe ingresar usuario y contraseña.",
                resultado);
        }

        [Fact]
        public void ValidarCampos_UsuarioMayorA50Caracteres_Rechaza()
        {
            var usuario = new string('a', 51);

            var resultado = ReglasLogin.ValidarCampos(
                usuario, "123456");

            Assert.Contains("50 caracteres", resultado!);
        }

        [Fact]
        public void ValidarCampos_UsuarioCon50Caracteres_Acepta()
        {
            var usuario = new string('a', 50);

            var resultado = ReglasLogin.ValidarCampos(
                usuario, "123456");

            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarCampos_PasswordMayorA100Caracteres_Rechaza()
        {
            var password = new string('a', 101);

            var resultado = ReglasLogin.ValidarCampos(
                "usuario", password);

            Assert.Contains("100 caracteres", resultado!);
        }

        [Fact]
        public void ValidarCampos_PasswordCon100Caracteres_Acepta()
        {
            var password = new string('a', 100);

            var resultado = ReglasLogin.ValidarCampos(
                "usuario", password);

            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarCampos_DatosCorrectos_Acepta()
        {
            var resultado = ReglasLogin.ValidarCampos(
                "usuario", "123456");

            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarCampos_UsuarioConEspacios_Acepta()
        {
            var resultado = ReglasLogin.ValidarCampos(
                "  usuario  ", "123456");

            Assert.Null(resultado);
        }

        [Fact]
        public void NormalizarUsuario_EliminaEspaciosExternos()
        {
            var resultado = ReglasLogin.NormalizarUsuario(
                "  usuario  ");

            Assert.Equal("usuario", resultado);
        }

        [Fact]
        public void NormalizarUsuario_ConservaEspaciosInternos()
        {
            var resultado = ReglasLogin.NormalizarUsuario(
                "usuario prueba");

            Assert.Equal("usuario prueba", resultado);
        }

        [Fact]
        public void NormalizarUsuario_SinEspacios_NoModifica()
        {
            var resultado = ReglasLogin.NormalizarUsuario(
                "administrador");

            Assert.Equal("administrador", resultado);
        }
    }
}
