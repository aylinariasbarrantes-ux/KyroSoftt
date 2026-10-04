namespace SistemaReservasLaboratorios.Models
{
    public class ReservaCalendarioItemViewModel
    {
        public int Id { get; set; }
        public int LaboratorioId { get; set; }
        public string LaboratorioNombre { get; set; } = string.Empty;
        public string LaboratorioUbicacion { get; set; } = string.Empty;
        public string Responsable { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty; // yyyy-MM-dd
        public string HoraInicio { get; set; } = string.Empty; // HH:mm
        public string HoraFin { get; set; } = string.Empty; // HH:mm
        public string Estado { get; set; } = string.Empty;
    }

    public class ReservaCalendarioLaboratorioViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class ReservaCalendarioViewModel
    {
        public bool EsAdministrador { get; set; }
        public string FechaSeleccionada { get; set; } = string.Empty;
        public List<ReservaCalendarioItemViewModel> Reservas { get; set; } = new();
        public List<ReservaCalendarioLaboratorioViewModel> Laboratorios { get; set; } = new();
    }
}
