
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class MisReservasControllerTests
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
            string? usuario,
            string rol = "Usuario")
        {
            var sesion = new SesionPrueba();

            if (usuario != null)
                sesion.SetString("NombreUsuario", usuario);

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
                Nombre = "Laboratorio de Redes",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            return laboratorio;
        }

        private static Reserva AgregarReserva(
            AppDbContext contexto,
            int laboratorioId,
            string responsable,
            int dias,
            string estado = "Activa")
        {
            var reserva = new Reserva
            {
                LaboratorioId = laboratorioId,
                Responsable = responsable,
                Fecha = DateOnly.FromDateTime(
                    DateTime.Today.AddDays(dias)),
                HoraInicio = new TimeOnly(9, 0),
                HoraFin = new TimeOnly(11, 0),
                Estado = estado
            };

            contexto.Reservas.Add(reserva);
            contexto.SaveChanges();

            return reserva;
        }

        private static List<MiReservaItemViewModel> ObtenerModelo(
            IActionResult resultado)
        {
            var vista = Assert.IsType<ViewResult>(resultado);

            return Assert.IsType<List<MiReservaItemViewModel>>(
                vista.Model);
        }

        // HU7-11: muestra las reservas del usuario en sesión.

        [Fact]
        public void MisReservas_UsuarioConReservas_MuestraSusReservas()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", 7);
            AgregarReserva(contexto, laboratorio.Id, "aylin", 8);

            var controlador = CrearControlador(contexto, "aylin");

            var modelo = ObtenerModelo(controlador.MisReservas());

            Assert.Equal(2, modelo.Count);
        }

        // HU7-12: no muestra reservas de otros usuarios.

        [Fact]
        public void MisReservas_ReservasAjenas_NoLasMuestra()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", 7);
            AgregarReserva(contexto, laboratorio.Id, "maria", 8);

            var controlador = CrearControlador(contexto, "aylin");

            var modelo = ObtenerModelo(controlador.MisReservas());

            Assert.Single(modelo);
            Assert.Equal(
                DateOnly.FromDateTime(DateTime.Today.AddDays(7))
                    .ToString("yyyy-MM-dd"),
                modelo[0].Fecha);
        }

        // HU7-13: devuelve una lista vacía sin reservas.

        [Fact]
        public void MisReservas_UsuarioSinReservas_DevuelveVistaVacia()
        {
            using var contexto = CrearContexto();

            var controlador = CrearControlador(contexto, "aylin");

            var modelo = ObtenerModelo(controlador.MisReservas());

            Assert.Empty(modelo);
        }

        // HU7-14: incluso un administrador ve solo sus reservas.

        [Fact]
        public void MisReservas_Administrador_NoVeReservasAjenas()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "admin", 7);
            AgregarReserva(contexto, laboratorio.Id, "maria", 8);

            var controlador = CrearControlador(
                contexto, "admin", "Administrador");

            var modelo = ObtenerModelo(controlador.MisReservas());

            Assert.Single(modelo);
        }

        // HU7-15: una reserva futura activa permite cancelar.

        [Fact]
        public void MisReservas_ReservaActivaFutura_PermiteCancelar()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", 7);

            var controlador = CrearControlador(contexto, "aylin");

            var reserva = Assert.Single(
                ObtenerModelo(controlador.MisReservas()));

            Assert.Equal("Activa", reserva.Estado);
            Assert.True(reserva.PuedeCancelar);
        }

        // HU7-16: una reserva activa pasada se muestra expirada.

        [Fact]
        public void MisReservas_ReservaPasada_SeMuestraExpirada()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(contexto, laboratorio.Id, "aylin", -7);

            var controlador = CrearControlador(contexto, "aylin");

            var reserva = Assert.Single(
                ObtenerModelo(controlador.MisReservas()));

            Assert.Equal("Expirada", reserva.Estado);
            Assert.False(reserva.PuedeCancelar);
        }

        // HU7-17: una reserva cancelada no puede cancelarse otra vez.

        [Fact]
        public void MisReservas_ReservaCancelada_NoPermiteCancelar()
        {
            using var contexto = CrearContexto();
            var laboratorio = CrearLaboratorio(contexto);

            AgregarReserva(
                contexto, laboratorio.Id, "aylin", 7, "Cancelada");

            var controlador = CrearControlador(contexto, "aylin");

            var reserva = Assert.Single(
                ObtenerModelo(controlador.MisReservas()));

            Assert.Equal("Cancelada", reserva.Estado);
            Assert.False(reserva.PuedeCancelar);
        }

        // HU7-18: si no existe usuario en sesión, rechaza la consulta.

        [Fact]
        public void MisReservas_SinUsuarioEnSesion_DevuelveBadRequest()
        {
            using var contexto = CrearContexto();

            var controlador = CrearControlador(contexto, null);

            var resultado = controlador.MisReservas();

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        private sealed class CaracteristicaSesionPrueba : ISessionFeature
        {
            public ISession Session { get; set; } = default!;
        }

        private sealed class SesionPrueba : ISession
        {
            private readonly Dictionary<string, byte[]> _datos = new();

            public bool IsAvailable => true;

            public string Id => "sesion-prueba";

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
