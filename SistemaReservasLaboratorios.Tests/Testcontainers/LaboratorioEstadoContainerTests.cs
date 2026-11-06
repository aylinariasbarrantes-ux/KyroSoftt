
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Testcontainers;

public class LaboratorioEstadoContainerTests
    : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public LaboratorioEstadoContainerTests(
        SqlServerContainerFixture fixture)
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
    public async Task DeshabilitarLaboratorio_DebePersistirEstado()
    {
        // ARRANGE: crear laboratorio habilitado.
        int laboratorioId;

        await using (var context = CrearContexto())
        {
            var laboratorio = new Laboratorio
            {
                Nombre = $"Lab-{Guid.NewGuid():N}",
                Ubicacion = "Edificio de pruebas",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            context.Laboratorios.Add(laboratorio);
            await context.SaveChangesAsync();

            laboratorioId = laboratorio.Id;
        }

        // ACT: actualizar su estado en SQL Server.
        await using (var context = CrearContexto())
        {
            var laboratorio = await context.Laboratorios
                .SingleAsync(l => l.Id == laboratorioId);

            laboratorio.Estado = "Fuera de servicio";

            await context.SaveChangesAsync();
        }

        // ASSERT: comprobar el cambio desde otra conexión.
        await using (var context = CrearContexto())
        {
            var laboratorio = await context.Laboratorios
                .AsNoTracking()
                .SingleAsync(l => l.Id == laboratorioId);

            Assert.Equal("Fuera de servicio", laboratorio.Estado);
        }
    }

    [Fact]
    public async Task LaboratorioDeshabilitado_NoDebePermitirReservas()
    {
        // ARRANGE: crear laboratorio habilitado.
        int laboratorioId;

        await using (var context = CrearContexto())
        {
            var laboratorio = new Laboratorio
            {
                Nombre = $"Lab-{Guid.NewGuid():N}",
                Ubicacion = "Edificio de pruebas",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            context.Laboratorios.Add(laboratorio);
            await context.SaveChangesAsync();

            laboratorioId = laboratorio.Id;
        }

        // Deshabilitar el laboratorio.
        await using (var context = CrearContexto())
        {
            var laboratorio = await context.Laboratorios
                .SingleAsync(l => l.Id == laboratorioId);

            laboratorio.Estado = "Fuera de servicio";

            await context.SaveChangesAsync();
        }

        // ACT: intentar reservar mediante el servicio real.
        await using (var context = CrearContexto())
        {
            var servicio = new ReservaService(context);

            var resultado = servicio.RegistrarReserva(
                laboratorioId,
                DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                new TimeOnly(8, 0),
                new TimeOnly(10, 0),
                "usuario-prueba");

            Assert.False(resultado.Disponible);
        }

        // ASSERT: comprobar que no se guardó ninguna reserva.
        await using (var context = CrearContexto())
        {
            var cantidad = await context.Reservas
                .CountAsync(r => r.LaboratorioId == laboratorioId);

            Assert.Equal(0, cantidad);
        }
    }
}
