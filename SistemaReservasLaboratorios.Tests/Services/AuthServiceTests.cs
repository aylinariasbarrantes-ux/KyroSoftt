using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;
using Moq;

namespace SistemaReservasLaboratorios.Tests.Services
{
    public class AuthServiceTests
    {
        // Crea un AppDbContext nuevo, en memoria, aislado para cada prueba
        private static AppDbContext CrearContextoEnMemoria(string nombreBd)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: nombreBd)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public void ValidarCredenciales_ConCredencialesValidas_RetornaUsuario()
        {
            // Arrange
            var hasher = new PasswordHasher<Usuario>();
            using var context = CrearContextoEnMemoria(nameof(ValidarCredenciales_ConCredencialesValidas_RetornaUsuario));

            var usuario = new Usuario { NombreUsuario = "admin", Rol = "Administrador" };
            usuario.PasswordHash = hasher.HashPassword(usuario, "admin123");
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            var authService = new AuthService(context, hasher);

            // Act
            var resultado = authService.ValidarCredenciales("admin", "admin123");

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("admin", resultado!.NombreUsuario);
            Assert.Equal("Administrador", resultado.Rol);
        }

        [Fact]
        public void ValidarCredenciales_ConUsuarioInexistente_RetornaNull()
        {
            // Arrange
            var hasher = new PasswordHasher<Usuario>();
            using var context = CrearContextoEnMemoria(nameof(ValidarCredenciales_ConUsuarioInexistente_RetornaNull));
            var authService = new AuthService(context, hasher);

            // Act
            var resultado = authService.ValidarCredenciales("usuario_que_no_existe", "cualquierpass");

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarCredenciales_ConPasswordIncorrecta_RetornaNull()
        {
            // Arrange
            var hasher = new PasswordHasher<Usuario>();
            using var context = CrearContextoEnMemoria(nameof(ValidarCredenciales_ConPasswordIncorrecta_RetornaNull));

            var usuario = new Usuario { NombreUsuario = "usuario1", Rol = "Usuario" };
            usuario.PasswordHash = hasher.HashPassword(usuario, "user123");
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            var authService = new AuthService(context, hasher);

            // Act
            var resultado = authService.ValidarCredenciales("usuario1", "contraseña-incorrecta");

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarCredenciales_ConNombreUsuarioNulo_LanzaExcepcion()
        {
            // Arrange
            var hasher = new PasswordHasher<Usuario>();
            using var context = CrearContextoEnMemoria(nameof(ValidarCredenciales_ConNombreUsuarioNulo_LanzaExcepcion));

            // Necesitamos al menos un usuario para que el predicado del FirstOrDefault
            // se evalúe y provoque la excepción al llamar nombreUsuario.ToLower()
            var usuario = new Usuario { NombreUsuario = "admin", Rol = "Administrador" };
            usuario.PasswordHash = hasher.HashPassword(usuario, "admin123");
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            var authService = new AuthService(context, hasher);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                authService.ValidarCredenciales(null!, "cualquierpass"));
            Assert.IsType<NullReferenceException>(ex.InnerException);
        }

        [Fact]
        public void ValidarCredenciales_ConHashCorrupto_LanzaExcepcion()
        {
            // Arrange
            var hasher = new PasswordHasher<Usuario>();
            using var context = CrearContextoEnMemoria(nameof(ValidarCredenciales_ConHashCorrupto_LanzaExcepcion));

            // Un PasswordHash con formato inválido (no generado por el hasher real)
            var usuario = new Usuario { NombreUsuario = "corrupto", Rol = "Usuario", PasswordHash = "esto-no-es-un-hash-valido" };
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            var authService = new AuthService(context, hasher);

            // Act & Assert
            Assert.ThrowsAny<FormatException>(() =>
                authService.ValidarCredenciales("corrupto", "cualquierpass"));
        }

        [Fact]
        public void ValidarCredenciales_ConHashQueRequiereRehash_RetornaUsuario()
        {
            // Arrange
            using var context = CrearContextoEnMemoria(nameof(ValidarCredenciales_ConHashQueRequiereRehash_RetornaUsuario));

            var usuario = new Usuario { NombreUsuario = "admin", Rol = "Administrador", PasswordHash = "hash-simulado" };
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            // El hasher real no puede forzar este resultado, por eso se simula con Moq
            var hasherMock = new Mock<IPasswordHasher<Usuario>>();
            hasherMock
                .Setup(h => h.VerifyHashedPassword(It.IsAny<Usuario>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(PasswordVerificationResult.SuccessRehashNeeded);

            var authService = new AuthService(context, hasherMock.Object);

            // Act
            var resultado = authService.ValidarCredenciales("admin", "admin123");

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("admin", resultado!.NombreUsuario);
        }

        [Fact]
        public void ValidarCredenciales_ConNombreEnMayusculas_RetornaUsuario()
        {
            // Arrange
            var hasher = new PasswordHasher<Usuario>();
            using var context = CrearContextoEnMemoria(nameof(ValidarCredenciales_ConNombreEnMayusculas_RetornaUsuario));

            var usuario = new Usuario { NombreUsuario = "admin", Rol = "Administrador" };
            usuario.PasswordHash = hasher.HashPassword(usuario, "admin123");
            context.Usuarios.Add(usuario);
            context.SaveChanges();

            var authService = new AuthService(context, hasher);

            // Act
            var resultado = authService.ValidarCredenciales("ADMIN", "admin123");

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("admin", resultado!.NombreUsuario);
        }
    }
}