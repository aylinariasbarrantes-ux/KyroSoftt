
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Controllers;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Controllers
{
    public class LaboratorioControllerTests
    {
        private static AppDbContext CrearContexto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(opciones);
        }

        [Fact]
        public void Index_RetornaUnaVista()
        {
            // Arrange
            using var contexto = CrearContexto();
            var servicio = new LaboratorioService(contexto);
            var controlador = new LaboratorioController(servicio);

            // Act
            var resultado = controlador.Index();

            // Assert
            Assert.IsType<ViewResult>(resultado);
        }

        [Fact]
        public void Index_ConLaboratorios_RetornaListaCorrecta()
        {
            // Arrange
            using var contexto = CrearContexto();

            contexto.Laboratorios.Add(new Laboratorio
            {
                Nombre = "Laboratorio A",
                Ubicacion = "Edificio Central",
                Capacidad = 30,
                Estado = "Habilitado"
            });

            contexto.SaveChanges();

            var servicio = new LaboratorioService(contexto);
            var controlador = new LaboratorioController(servicio);

            // Act
            var resultado = controlador.Index();

            // Assert
            var vista = Assert.IsType<ViewResult>(resultado);

            var modelo = Assert.IsAssignableFrom<
                IEnumerable<Laboratorio>>(vista.Model);

            var laboratorio = Assert.Single(modelo);

            Assert.Equal("Laboratorio A", laboratorio.Nombre);
            Assert.Equal("Edificio Central", laboratorio.Ubicacion);
            Assert.Equal(30, laboratorio.Capacidad);
            Assert.Equal("Habilitado", laboratorio.Estado);
        }

        [Fact]
        public void Index_SinLaboratorios_RetornaListaVacia()
        {
            // Arrange
            using var contexto = CrearContexto();

            var servicio = new LaboratorioService(contexto);
            var controlador = new LaboratorioController(servicio);

            // Act
            var resultado = controlador.Index();

            // Assert
            var vista = Assert.IsType<ViewResult>(resultado);

            var modelo = Assert.IsAssignableFrom<
                IEnumerable<Laboratorio>>(vista.Model);

            Assert.Empty(modelo);
        }
    }
}
