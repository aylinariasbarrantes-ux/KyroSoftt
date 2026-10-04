using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Data
{
    public static class DbSeeder
    {
        public static void Seed(
            AppDbContext context,
            IPasswordHasher<Usuario> hasher)
        {
            context.Database.EnsureDeleted();
            context.Database.Migrate();

            var admin = new Usuario { NombreUsuario = "admin", Rol = "Administrador" };
            admin.PasswordHash = hasher.HashPassword(admin, "admin123");

            var usuario1 = new Usuario { NombreUsuario = "usuario1", Rol = "Usuario" };
            usuario1.PasswordHash = hasher.HashPassword(usuario1, "user123");

            context.Usuarios.AddRange(admin, usuario1);

            context.SaveChanges();

            var laboratorioA = new Laboratorio { Nombre = "Laboratorio A", Ubicacion = "Edificio 1, Piso 1", Capacidad = 25, Estado = "Habilitado" };
            var laboratorioB = new Laboratorio { Nombre = "Laboratorio B", Ubicacion = "Edificio 1, Piso 2", Capacidad = 20, Estado = "Habilitado" };
            var laboratorioC = new Laboratorio { Nombre = "Laboratorio C", Ubicacion = "Edificio 2, Piso 1", Capacidad = 30, Estado = "Fuera de servicio" };

            context.Laboratorios.AddRange(laboratorioA, laboratorioB, laboratorioC);

            context.SaveChanges();

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var lunes = hoy.DayOfWeek == DayOfWeek.Sunday ? hoy.AddDays(-6) : hoy.AddDays(1 - (int)hoy.DayOfWeek);

            context.Reservas.AddRange( 
                new Reserva { LaboratorioId = laboratorioA.Id, Responsable = "Ing. María Vargas", Fecha = lunes, HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(10, 30), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioB.Id, Responsable = "Lic. Andrea Solano", Fecha = lunes, HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(10, 30), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioC.Id, Responsable = "Proyecto Integrador", Fecha = lunes, HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(10, 30), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioB.Id, Responsable = "Prof. Daniel Rojas", Fecha = lunes.AddDays(1), HoraInicio = new TimeOnly(10, 0), HoraFin = new TimeOnly(12, 0), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioA.Id, Responsable = "Lic. Andrea Solano", Fecha = hoy, HoraInicio = new TimeOnly(13, 30), HoraFin = new TimeOnly(15, 30), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioC.Id, Responsable = "MSc. Kevin Mena", Fecha = hoy, HoraInicio = new TimeOnly(14, 0), HoraFin = new TimeOnly(16, 0), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioB.Id, Responsable = "MSc. Kevin Mena", Fecha = hoy, HoraInicio = new TimeOnly(16, 0), HoraFin = new TimeOnly(18, 0), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioA.Id, Responsable = "Grupo Bases de Datos", Fecha = hoy.AddDays(1), HoraInicio = new TimeOnly(9, 0),HoraFin = new TimeOnly(11, 30), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioB.Id, Responsable = "Curso de Redes", Fecha = hoy.AddDays(3), HoraInicio = new TimeOnly(14, 0), HoraFin = new TimeOnly(17, 0), Estado = "Activa" },
                new Reserva { LaboratorioId = laboratorioA.Id, Responsable = "Proyecto Integrador", Fecha = new DateOnly( hoy.Year, hoy.Month, Math.Min(DateTime.DaysInMonth(hoy.Year, hoy.Month), 22)), HoraInicio = new TimeOnly(8, 30), HoraFin = new TimeOnly(12, 0), Estado = "Activa" }
            );

            context.SaveChanges();
        }
    }
}