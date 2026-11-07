
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using SistemaReservasLaboratorios.Tests.Infrastructure.Fixtures;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Integration.Testcontainers;

public class ReservaServiceContainerTests
    : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public ReservaServiceContainerTests(
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

    private static DateOnly FechaFutura()
    {
        return DateOnly.FromDateTime(DateTime.Today.AddDays(30));
    }

    private static Laboratorio NuevoLaboratorio(
        string estado = "Habilitado")
    {
        return new Laboratorio
        {
            Nombre = $"Laboratorio-{Guid.NewGuid():N}",
            Ubicacion = "Edificio de pruebas",
            Capacidad = 25,
            Estado = estado
        };
    }

    [Fact]
    public async Task ConsultarDisponibilidad_LaboratorioLibre_DebeEstarDisponible()
    {
        // ARRANGE: guardar un laboratorio en SQL Server.
        int laboratorioId;

        await using (var context = CrearContexto())
        {
            var laboratorio = NuevoLaboratorio();

            context.Laboratorios.Add(laboratorio);
            await context.SaveChangesAsync();

            laboratorioId = laboratorio.Id;
        }

        // ACT: consultar mediante el servicio real.
        using var consulta = CrearContexto();
        var servicio = new ReservaService(consulta);

        var resultado = servicio.ConsultarDisponibilidad(
            laboratorioId,
            FechaFutura(),
            new TimeOnly(8, 0),
            new TimeOnly(10, 0));

        // ASSERT
        Assert.True(resultado.Disponible);
    }

    [Fact]
    public async Task RegistrarReserva_DatosValidos_DebePersistirEnSqlServer()
    {
        // ARRANGE
        int laboratorioId;
        var fecha = FechaFutura();
        var responsable = $"usuario-{Guid.NewGuid():N}";

        await using (var context = CrearContexto())
        {
            var laboratorio = NuevoLaboratorio();

            context.Laboratorios.Add(laboratorio);
            await context.SaveChangesAsync();

            laboratorioId = laboratorio.Id;
        }

        // ACT: registrar mediante ReservaService.
        using (var context = CrearContexto())
        {
            var servicio = new ReservaService(context);

            var resultado = servicio.RegistrarReserva(
                laboratorioId,
                fecha,
                new TimeOnly(8, 0),
                new TimeOnly(10, 0),
                responsable);

            Assert.True(resultado.Disponible);
        }

        // ASSERT: consultar desde un contexto nuevo.
        await using (var verificacion = CrearContexto())
        {
            var reserva = await verificacion.Reservas
                .AsNoTracking()
                .SingleOrDefaultAsync(r =>
                    r.LaboratorioId == laboratorioId &&
                    r.Responsable == responsable);

            Assert.NotNull(reserva);
            Assert.Equal(fecha, reserva.Fecha);
            Assert.Equal(new TimeOnly(8, 0), reserva.HoraInicio);
            Assert.Equal(new TimeOnly(10, 0), reserva.HoraFin);
            Assert.Equal("Activa", reserva.Estado);
        }
    }

    [Fact]
    public async Task RegistrarReserva_HorarioOcupado_DebeRechazarTraslape()
    {
        // ARRANGE
        int laboratorioId;
        var fecha = FechaFutura();

        await using (var context = CrearContexto())
        {
            var laboratorio = NuevoLaboratorio();

            context.Laboratorios.Add(laboratorio);
            await context.SaveChangesAsync();

            laboratorioId = laboratorio.Id;
        }

        // Primera reserva: 8:00 a 10:00.
        using (var context = CrearContexto())
        {
            var servicio = new ReservaService(context);

            var primera = servicio.RegistrarReserva(
                laboratorioId,
                fecha,
                new TimeOnly(8, 0),
                new TimeOnly(10, 0),
                "responsable-original");

            Assert.True(primera.Disponible);
        }

        // ACT: intentar reservar de 9:00 a 11:00.
        using (var context = CrearContexto())
        {
            var servicio = new ReservaService(context);

            var segunda = servicio.RegistrarReserva(
                laboratorioId,
                fecha,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0),
                "responsable-segundo");

            Assert.False(segunda.Disponible);
        }

        // ASSERT: únicamente debe existir una reserva.
        await using (var verificacion = CrearContexto())
        {
            var cantidad = await verificacion.Reservas
                .CountAsync(r =>
                    r.LaboratorioId == laboratorioId &&
                    r.Fecha == fecha);

            Assert.Equal(1, cantidad);
        }
    }

    [Fact]
    public async Task RegistrarReserva_LaboratorioFueraDeServicio_DebeRechazar()
    {
        // ARRANGE
        int laboratorioId;
        var fecha = FechaFutura();

        await using (var context = CrearContexto())
        {
            var laboratorio =
                NuevoLaboratorio("Fuera de servicio");

            context.Laboratorios.Add(laboratorio);
            await context.SaveChangesAsync();

            laboratorioId = laboratorio.Id;
        }

        // ACT
        using (var context = CrearContexto())
        {
            var servicio = new ReservaService(context);

            var resultado = servicio.RegistrarReserva(
                laboratorioId,
                fecha,
                new TimeOnly(8, 0),
                new TimeOnly(10, 0),
                "responsable-prueba");

            Assert.False(resultado.Disponible);
        }

        // ASSERT: no se debe guardar ninguna reserva.
        await using (var verificacion = CrearContexto())
        {
            var cantidad = await verificacion.Reservas
                .CountAsync(r => r.LaboratorioId == laboratorioId);

            Assert.Equal(0, cantidad);
        }
    }
}
