namespace SistemaReservasLaboratorios.Models
{
    public class Reserva
    {
        public int Id { get; set; }
        public int LaboratorioId { get; set; }
        public Laboratorio? Laboratorio { get; set; }
        public string Responsable { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public string Estado { get; set; } = "Activa"; // "Activa" o "Cancelada"
    }
}