using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;

namespace SistemaReservasLaboratorios.Tests.Integration;

// Integra el servicio real, EF Core, SQLite relacional y el hasher real.
// Cada caso usa su propia conexión. No necesita SQL Server ni cambia sus datos.
public class AuthDatabaseIntegrationTests
{
    [Theory]
    [InlineData("ClavePrueba123!", true)]
    [InlineData("incorrecta", false)]
    [Trait("Categoria", "Integracion")]
    public void ValidarCredenciales_LeeUsuarioPersistido_YVerificaPassword(
        string password, bool esperado)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options;
        var hasher = new PasswordHasher<Usuario>();

        // Guarda con un contexto y consulta con otro: obliga a leer la base real.
        using (var setup = new AppDbContext(options))
        {
            setup.Database.EnsureCreated();
            var usuario = new Usuario { NombreUsuario = "integracion", Rol = "Usuario" };
            usuario.PasswordHash = hasher.HashPassword(usuario, "ClavePrueba123!");
            setup.Usuarios.Add(usuario);
            setup.SaveChanges();
        }

        using var context = new AppDbContext(options);
        var service = new AuthService(context, hasher);
        var resultado = service.ValidarCredenciales("INTEGRACION", password);

        if (esperado)
        {
            Assert.NotNull(resultado);
            Assert.Equal("integracion", resultado!.NombreUsuario);
            Assert.Equal("Usuario", resultado.Rol);
            Assert.True(resultado.Id > 0);
        }
        else
        {
            Assert.Null(resultado);
        }
    }
}
