
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using Testcontainers.MsSql;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Infrastructure.Fixtures;

public class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container =
     new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
         .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        // Iniciar SQL Server dentro de Docker.
        await _container.StartAsync();

        // Crear el contexto conectado al contenedor.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new AppDbContext(options);

        // Aplicar las migraciones del proyecto.
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // Eliminar el contenedor al terminar las pruebas.
        await _container.DisposeAsync();
    }
}
