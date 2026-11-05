
using System;
using System.Globalization;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Services
{
    public static class ReglasFormularioReserva
    {
        public static DisponibilidadResultado? ParsearHorario(
            int laboratorioId,
            string? fecha,
            string? horaInicio,
            string? horaFin,
            out DateOnly fechaParseada,
            out TimeOnly inicioParseado,
            out TimeOnly finParseado)
        {
            fechaParseada = default;
            inicioParseado = default;
            finParseado = default;

            if (laboratorioId <= 0)
            {
                return DisponibilidadResultado.NoDisponible(
                    "Debe indicar un laboratorio válido.");
            }

            if (!DateOnly.TryParseExact(
                fecha,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fechaParseada))
            {
                return DisponibilidadResultado.NoDisponible(
                    "La fecha no es válida. Usa el formato yyyy-MM-dd.");
            }

            if (!TimeOnly.TryParseExact(
                horaInicio,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out inicioParseado))
            {
                return DisponibilidadResultado.NoDisponible(
                    "La hora de inicio no es válida. Usa el formato HH:mm.");
            }

            if (!TimeOnly.TryParseExact(
                horaFin,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out finParseado))
            {
                return DisponibilidadResultado.NoDisponible(
                    "La hora de fin no es válida. Usa el formato HH:mm.");
            }

            if (!EsIntervaloDeTreintaMinutos(inicioParseado) ||
                !EsIntervaloDeTreintaMinutos(finParseado))
            {
                return DisponibilidadResultado.NoDisponible(
                    "Las horas deben seleccionarse en intervalos de 30 minutos (:00 o :30).");
            }

            if (inicioParseado >= finParseado)
            {
                return DisponibilidadResultado.NoDisponible(
                    "La hora final debe ser posterior a la hora inicial.");
            }

            return null;
        }

        private static bool EsIntervaloDeTreintaMinutos(TimeOnly hora)
        {
            return hora.Minute == 0 || hora.Minute == 30;
        }
    }
}
