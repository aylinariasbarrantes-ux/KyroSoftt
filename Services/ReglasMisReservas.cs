
using System;

namespace SistemaReservasLaboratorios.Services
{
    public static class ReglasMisReservas
    {
        public static string ObtenerEstadoVisible(
            string estado,
            DateOnly fecha,
            TimeOnly horaFin,
            DateTime ahora)
        {
            var estaActiva = string.Equals(
                estado,
                "Activa",
                StringComparison.OrdinalIgnoreCase);

            if (estaActiva &&
                ReglasCancelacion.EstaExpirada(
                    fecha,
                    horaFin,
                    ahora))
            {
                return "Expirada";
            }

            return estado;
        }

        public static bool PuedeMostrarCancelacion(
            string estado,
            DateOnly fecha,
            TimeOnly horaFin,
            DateTime ahora)
        {
            var estaActiva = string.Equals(
                estado,
                "Activa",
                StringComparison.OrdinalIgnoreCase);

            return estaActiva &&
                   !ReglasCancelacion.EstaExpirada(
                       fecha,
                       horaFin,
                       ahora);
        }
    }
}
