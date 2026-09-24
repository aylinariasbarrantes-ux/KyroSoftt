using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Laboratorio> Laboratorios { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        // Sin datos semilla aquí: los usuarios se hashean y los laboratorios
        // se siembran en DbSeeder.cs al arrancar la app.
    }
}