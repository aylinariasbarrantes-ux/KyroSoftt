namespace SistemaReservasLaboratorios.Models
{
    // Resultado simple de una operación (ej. cancelar): éxito o error con un mensaje para el usuario.
    public record OperacionResultado(bool Exito, string Mensaje)
    {
        public static OperacionResultado Ok(string mensaje) => new(true, mensaje);
        public static OperacionResultado Error(string mensaje) => new(false, mensaje);
    }
}