
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.UNIT.Services
{
    public class LaboratorioServiceTests
    {
        private static AppDbContext CrearContexto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(opciones);
        }

        [Fact]
        public void ListarLaboratorios_ConRegistros_RetornaTodosOrdenados()
        {
            // Arrange
            using var contexto = CrearContexto();

            contexto.Laboratorios.AddRange(
                new Laboratorio
                {
                    Nombre = "Laboratorio Z",
                    Ubicacion = "Edificio B",
                    Capacidad = 20,
                    Estado = "Habilitado"
                },
                new Laboratorio
                {
                    Nombre = "Laboratorio A",
                    Ubicacion = "Edificio A",
                    Capacidad = 30,
                    Estado = "Fuera de servicio"
                }
            );

            contexto.SaveChanges();
            var servicio = new LaboratorioService(contexto);

            // Act
            var resultado = servicio.ListarLaboratorios();

            // Assert
            Assert.Equal(2, resultado.Count);
            Assert.Equal("Laboratorio A", resultado[0].Nombre);
            Assert.Equal("Laboratorio Z", resultado[1].Nombre);
        }

        [Fact]
        public void ListarLaboratorios_SinRegistros_RetornaListaVacia()
        {
            // Arrange
            using var contexto = CrearContexto();
            var servicio = new LaboratorioService(contexto);

            // Act
            var resultado = servicio.ListarLaboratorios();

            // Assert
            Assert.Empty(resultado);
        }

        [Fact]
        public void ListarLaboratorios_ConEstadosDiferentes_ConservaInformacion()
        {
            // Arrange
            using var contexto = CrearContexto();

            contexto.Laboratorios.Add(new Laboratorio
            {
                Nombre = "Laboratorio 1",
                Ubicacion = "Segundo piso",
                Capacidad = 25,
                Estado = "Fuera de servicio"
            });

            contexto.SaveChanges();
            var servicio = new LaboratorioService(contexto);

            // Act
            var resultado = servicio.ListarLaboratorios();

            // Assert
            var laboratorio = Assert.Single(resultado);

            Assert.Equal("Laboratorio 1", laboratorio.Nombre);
            Assert.Equal("Segundo piso", laboratorio.Ubicacion);
            Assert.Equal(25, laboratorio.Capacidad);
            Assert.Equal("Fuera de servicio", laboratorio.Estado);
        }

        [Fact]
        public void ListarLaboratorios_NoRealizaSeguimientoDeEntidades()
        {
            // Arrange
            using var contexto = CrearContexto();

            contexto.Laboratorios.Add(new Laboratorio
            {
                Nombre = "Laboratorio A"
            });

            contexto.SaveChanges();
            contexto.ChangeTracker.Clear();

            var servicio = new LaboratorioService(contexto);

            // Act
            var resultado = servicio.ListarLaboratorios();

            // Assert
            Assert.Single(resultado);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
    }
}
