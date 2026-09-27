using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Services
{
    public class LaboratorioService
    {
        private readonly AppDbContext _context;

        public LaboratorioService(AppDbContext context)
        {
            _context = context;
        }

        // Devuelve todos los laboratorios ordenados por nombre, incluido su Estado
        public List<Laboratorio> ListarLaboratorios()
        {
            return _context.Laboratorios
                .AsNoTracking() // Solo lectura: evita rastrear entidades sin modificar
                .OrderBy(l => l.Nombre)
                .ToList();
        }
    }
}
