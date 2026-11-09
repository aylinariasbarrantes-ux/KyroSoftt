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


        public OperacionResultado CambiarEstado(int laboratorioId, string nuevoEstado)
        {
            // Solo se permiten los dos estados definidos para HU-9.
            if (nuevoEstado != "Habilitado" &&
                nuevoEstado != "Fuera de servicio")
            {
                return OperacionResultado.Error(
                    "El estado seleccionado no es válido.");
            }

            // Buscar el laboratorio en la base de datos.
            var laboratorio = _context.Laboratorios
                .FirstOrDefault(l => l.Id == laboratorioId);

            if (laboratorio == null)
            {
                return OperacionResultado.Error(
                    "El laboratorio no existe.");
            }

            if (laboratorio.Estado == nuevoEstado)
            {
                return OperacionResultado.Error(
                    "El laboratorio ya tiene ese estado.");
            }

            // Actualizar el estado y guardar permanentemente.
            laboratorio.Estado = nuevoEstado;
            _context.SaveChanges();

            return OperacionResultado.Ok(
                $"El laboratorio se actualizó a: {nuevoEstado}.");
        }

    }
}
