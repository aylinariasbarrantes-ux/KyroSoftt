namespace SistemaReservasLaboratorios.Models
{
    // Resultado de consultar si un laboratorio está libre en un rango horario.
    // No es una entidad de EF: es solo la respuesta que devuelve ReservaService.
    // Motivo va vacío cuando Disponible es true, y explica el motivo cuando es false.
    public record DisponibilidadResultado(bool Disponible, string Motivo)
    {
        // Atajo para el caso "sí está disponible", donde no hay motivo que explicar
        public static DisponibilidadResultado Libre() => new(true, string.Empty);

        public static DisponibilidadResultado NoDisponible(string motivo) => new(false, motivo);
    }
}
