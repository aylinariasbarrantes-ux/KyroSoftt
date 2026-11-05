
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Controllers;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;
using SistemaReservasLaboratorios.Services;
using Xunit;

namespace SistemaReservasLaboratorios.Tests.Controllers
{
    public class CancelarReservaControllerTests
    {
        private static AppDbContext CrearContexto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(opciones);
        }

        private static ReservaController CrearControlador(
            AppDbContext contexto,
            string? usuario = "aylin",
            string rol = "Usuario")
        {
            var sesion = new SesionPrueba();

            if (usuario != null)
            {
                sesion.SetString("NombreUsuario", usuario);
            }

            sesion.SetString("Rol", rol);

            var httpContext = new DefaultHttpContext();

            httpContext.Features.Set<ISessionFeature>(
                new CaracteristicaSesionPrueba
                {
                    Session = sesion
                });

            return new ReservaController(
                new ReservaService(contexto),
                new LaboratorioService(contexto))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        private static Laboratorio CrearLaboratorio(
            AppDbContext contexto)
        {
            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio HU8",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio;
        }

        private static Reserva CrearReserva(
            AppDbContext contexto,
            int laboratorioId,
            string responsable = "aylin",
            int diasDesdeHoy = 7,
            string estado = "Activa")
        {
            var reserva = new Reserva
            {
                LaboratorioId = laboratorioId,
                Responsable = responsable,
                Fecha = DateOnly.FromDateTime(
                    DateTime.Today.AddDays(diasDesdeHoy)),
                HoraInicio = new TimeOnly(9, 0),
                HoraFin = new TimeOnly(11, 0),
                Estado = estado
            };

            contexto.Reservas.Add(reserva);
            contexto.SaveChanges();

            return reserva;
        }

        // HU8-13: El propietario puede cancelar su reserva.

        [Fact]
        public void CancelarReserva_Propietario_DevuelveOk()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var reserva = CrearReserva(contexto, laboratorio.Id);

            var controlador = CrearControlador(contexto, "aylin");

            var resultado = controlador.CancelarReserva(reserva.Id);

            Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("Cancelada", reserva.Estado);
        }

        // HU8-14: Un usuario no puede cancelar una reserva ajena.

        [Fact]
        public void CancelarReserva_UsuarioAjeno_DevuelveBadRequest()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var reserva = CrearReserva(
                contexto, laboratorio.Id, "maria");

            var controlador = CrearControlador(contexto, "aylin");

            var resultado = controlador.CancelarReserva(reserva.Id);

            Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal("Activa", reserva.Estado);
        }

        // HU8-15: Un administrador puede cancelar una reserva ajena.

        [Fact]
        public void CancelarReserva_Administrador_DevuelveOk()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var reserva = CrearReserva(
                contexto, laboratorio.Id, "maria");

            var controlador = CrearControlador(
                contexto, "admin", "Administrador");

            var resultado = controlador.CancelarReserva(reserva.Id);

            Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("Cancelada", reserva.Estado);
        }

        // HU8-16: Un ID cero debe rechazarse.

        [Fact]
        public void CancelarReserva_IdCero_DevuelveBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.CancelarReserva(0);

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        // HU8-17: Un ID negativo debe rechazarse.

        [Fact]
        public void CancelarReserva_IdNegativo_DevuelveBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.CancelarReserva(-5);

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        // HU8-18: No se puede cancelar sin usuario en sesión.

        [Fact]
        public void CancelarReserva_SinUsuario_DevuelveBadRequest()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var reserva = CrearReserva(contexto, laboratorio.Id);

            var controlador = CrearControlador(
                contexto, usuario: null);

            var resultado = controlador.CancelarReserva(reserva.Id);

            Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal("Activa", reserva.Estado);
        }

        // HU8-19: Una reserva inexistente devuelve error.

        [Fact]
        public void CancelarReserva_ReservaInexistente_DevuelveBadRequest()
        {
            using var contexto = CrearContexto();
            var controlador = CrearControlador(contexto);

            var resultado = controlador.CancelarReserva(99999);

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        // HU8-20: Una reserva cancelada no se cancela de nuevo.

        [Fact]
        public void CancelarReserva_YaCancelada_DevuelveBadRequest()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);
            var reserva = CrearReserva(
                contexto, laboratorio.Id, "aylin", 7, "Cancelada");

            var controlador = CrearControlador(contexto);

            var resultado = controlador.CancelarReserva(reserva.Id);

            Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal("Cancelada", reserva.Estado);
        }

        private sealed class CaracteristicaSesionPrueba : ISessionFeature
        {
            public ISession Session { get; set; } = default!;
        }

        private sealed class SesionPrueba : ISession
        {
            private readonly Dictionary<string, byte[]> _datos = new();

            public bool IsAvailable => true;

            public string Id => "sesion-hu8";

            public IEnumerable<string> Keys => _datos.Keys;

            public void Clear()
            {
                _datos.Clear();
            }

            public Task CommitAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task LoadAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void Remove(string key)
            {
                _datos.Remove(key);
            }

            public void Set(string key, byte[] value)
            {
                _datos[key] = value;
            }

            public bool TryGetValue(string key, out byte[] value)
            {
                return _datos.TryGetValue(key, out value!);
            }
        }
    }
}
