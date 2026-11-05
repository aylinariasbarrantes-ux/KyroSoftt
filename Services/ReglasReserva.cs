
using System;

namespace SistemaReservasLaboratorios.Services
{
    public static class ReglasReserva
    {
        private const int DuracionMaximaMinutos = 240;

        private static readonly TimeOnly HoraApertura = new(8, 0);
        private static readonly TimeOnly HoraCierre = new(22, 0);

        public static string? ValidarHorario(
            DateOnly fecha,
            TimeOnly horaInicio,
            TimeOnly horaFin,
            DateOnly hoy)
        {
            if (!EsIntervaloDeTreintaMinutos(horaInicio) ||
                !EsIntervaloDeTreintaMinutos(horaFin))
            {
                return "Las horas deben estar en intervalos de 30 minutos (:00 o :30).";
            }

            if (horaInicio >= horaFin)
            {
                return "La hora final debe ser posterior a la hora inicial.";
            }

            if (horaInicio < HoraApertura || horaFin > HoraCierre)
            {
                return "El horario permitido es de 8:00 a. m. a 10:00 p. m.";
            }

            var duracion =
                horaFin.ToTimeSpan() - horaInicio.ToTimeSpan();

            if (duracion.TotalMinutes > DuracionMaximaMinutos)
            {
                return "La reserva no puede superar las 4 horas (8 intervalos de 30 minutos).";
            }

            if (fecha < hoy)
            {
                return "La fecha seleccionada no puede estar en el pasado.";
            }

            return null;
        }

        public static bool HayTraslape(
            TimeOnly inicioExistente,
            TimeOnly finExistente,
            TimeOnly inicioNuevo,
            TimeOnly finNuevo)
        {
            return inicioNuevo < finExistente &&
                   finNuevo > inicioExistente;
        }

        private static bool EsIntervaloDeTreintaMinutos(
            TimeOnly hora)
        {
            return hora.Minute == 0 || hora.Minute == 30;
        }
    }
}
