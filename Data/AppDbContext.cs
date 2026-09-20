using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }

        // Sin datos semilla: las contraseñas se hashean al arrancar (salt aleatorio)
        // y HasData generaría una migración nueva en cada arranque.
    }
}
