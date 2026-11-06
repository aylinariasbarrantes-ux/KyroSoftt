
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Testcontainers;

public class AuthServiceContainerTests
    : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public AuthServiceContainerTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CrearUsuarioYLogin_CredencialesCorrectas_DebeAutenticar()
    {
        // ARRANGE: crear un usuario real en SQL Server.
        var nombre = $"usuario-{Guid.NewGuid():N}";
        const string password = "ClavePrueba123!";

        var hasher = new PasswordHasher<Usuario>();

        await using (var context = CrearContexto())
        {
            var usuario = new Usuario
            {
                NombreUsuario = nombre,
                Rol = "Usuario"
            };

            usuario.PasswordHash =
                hasher.HashPassword(usuario, password);

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
        }

        // ACT: autenticar desde un contexto diferente.
        await using var contextoLogin = CrearContexto();

        var authService = new AuthService(contextoLogin, hasher);

        var resultado = authService.ValidarCredenciales(
            nombre,
            password);

        // ASSERT
        Assert.NotNull(resultado);
        Assert.Equal(nombre, resultado.NombreUsuario);
        Assert.Equal("Usuario", resultado.Rol);
    }

    [Fact]
    public async Task Login_ContrasenaIncorrecta_DebeRechazar()
    {
        // ARRANGE
        var nombre = $"usuario-{Guid.NewGuid():N}";
        var hasher = new PasswordHasher<Usuario>();

        await using (var context = CrearContexto())
        {
            var usuario = new Usuario
            {
                NombreUsuario = nombre,
                Rol = "Usuario"
            };

            usuario.PasswordHash =
                hasher.HashPassword(usuario, "ClaveCorrecta123!");

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
        }

        // ACT
        await using var contextoLogin = CrearContexto();

        var servicio = new AuthService(contextoLogin, hasher);

        var resultado = servicio.ValidarCredenciales(
            nombre,
            "ClaveIncorrecta");

        // ASSERT
        Assert.Null(resultado);
    }
}
