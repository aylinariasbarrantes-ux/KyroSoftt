
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Tests.Infrastructure.Fixtures;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Integration.Testcontainers;

public class SqlServerIntegrationTests
    : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public SqlServerIntegrationTests(
        SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BaseDeDatos_DebePermitirConsultarLaboratorios()
    {
        // Arrange: conectar al SQL Server real del contenedor.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        await using var context = new AppDbContext(options);

        // Act: comprobar la conexión y consultar la tabla.
        var conexionDisponible =
            await context.Database.CanConnectAsync();

        var cantidadLaboratorios =
            await context.Laboratorios.CountAsync();

        // Assert: la conexión funciona y la consulta se ejecuta.
        Assert.True(conexionDisponible);
        Assert.True(cantidadLaboratorios >= 0);
    }
}
