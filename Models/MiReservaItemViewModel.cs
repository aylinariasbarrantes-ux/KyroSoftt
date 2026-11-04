namespace SistemaReservasLaboratorios.Models
{
    // HU-7: elemento de "Mis reservas" que se envía al frontend como JSON.
    public class MiReservaItemViewModel
    {
        public int Id { get; set; }
        public int LaboratorioId { get; set; }
        public string Laboratorio { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty; // yyyy-MM-dd
        public string HoraInicio { get; set; } = string.Empty; // HH:mm
        public string HoraFin { get; set; } = string.Empty; // HH:mm
        public string Estado { get; set; } = string.Empty; // "Activa", "Cancelada" o "Expirada"
        public bool PuedeCancelar { get; set; }
    }
}
