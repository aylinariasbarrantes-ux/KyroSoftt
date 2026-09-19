using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SistemaReservasLaboratorios.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Usuarios semilla (para que no arranque vacío)
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario { Id = 1, NombreUsuario = "admin", Password = "admin123", Rol = "Administrador" },
                new Usuario { Id = 2, NombreUsuario = "usuario1", Password = "user123", Rol = "Usuario" }
            );
        }
    }
}