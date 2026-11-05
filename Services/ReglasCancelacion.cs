
using System;

namespace SistemaReservasLaboratorios.Services
{
    public static class ReglasCancelacion
    {
        public static bool PuedeCancelar(
            string responsable,
            string usuario,
            bool esAdministrador)
        {
            return esAdministrador ||
                   string.Equals(
                       responsable,
                       usuario,
                       StringComparison.OrdinalIgnoreCase);
        }

        public static bool EstaExpirada(
            DateOnly fecha,
            TimeOnly horaFin,
            DateTime ahora)
        {
            var hoy = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            return fecha < hoy ||
                   (fecha == hoy && horaFin <= horaActual);
        }
    }
}
