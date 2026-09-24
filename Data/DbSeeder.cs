using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context, IPasswordHasher<Usuario> hasher)
        {
            context.Database.Migrate();

            if (!context.Usuarios.Any())
            {
                var admin = new Usuario { NombreUsuario = "admin", Rol = "Administrador" };
                admin.PasswordHash = hasher.HashPassword(admin, "admin123");
                context.Usuarios.Add(admin);

                var usuario1 = new Usuario { NombreUsuario = "usuario1", Rol = "Usuario" };
                usuario1.PasswordHash = hasher.HashPassword(usuario1, "user123");
                context.Usuarios.Add(usuario1);

                context.SaveChanges();
            }

            if (!context.Laboratorios.Any())
            {
                context.Laboratorios.AddRange(
                    new Laboratorio { Nombre = "Laboratorio A", Ubicacion = "Edificio 1, Piso 1", Capacidad = 25, Estado = "Habilitado" },
                    new Laboratorio { Nombre = "Laboratorio B", Ubicacion = "Edificio 1, Piso 2", Capacidad = 20, Estado = "Habilitado" },
                    new Laboratorio { Nombre = "Laboratorio C", Ubicacion = "Edificio 2, Piso 1", Capacidad = 30, Estado = "Fuera de servicio" }
                );

                context.SaveChanges();
            }
        }
    }
}