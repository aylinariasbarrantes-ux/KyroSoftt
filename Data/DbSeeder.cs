using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Data
{
    // Crea/migra la base de datos y siembra los usuarios de prueba con hash.
    // Se ejecuta al arrancar la app, nunca durante las herramientas de migración.
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context, IPasswordHasher<Usuario> hasher)
        {
            // Aplica las migraciones pendientes (idempotente)
            context.Database.Migrate();

            // Solo siembra si la tabla está vacía, para no duplicar usuarios
            if (context.Usuarios.Any())
            {
                return;
            }

            var admin = new Usuario { NombreUsuario = "admin", Rol = "Administrador" };
            admin.PasswordHash = hasher.HashPassword(admin, "admin123");
            context.Usuarios.Add(admin);

            var usuario1 = new Usuario { NombreUsuario = "usuario1", Rol = "Usuario" };
            usuario1.PasswordHash = hasher.HashPassword(usuario1, "user123");
            context.Usuarios.Add(usuario1);

            context.SaveChanges();
        }
    }
}
