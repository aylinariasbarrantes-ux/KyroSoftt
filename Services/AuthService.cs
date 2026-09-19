using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Services
{
    public class AuthService
    {
        // Lista de usuarios en memoria (simula la base de datos)
        private readonly List<Usuario> _usuarios = new()
        {
            new Usuario { Id = 1, NombreUsuario = "admin", Password = "admin123", Rol = "Administrador" },
            new Usuario { Id = 2, NombreUsuario = "usuario1", Password = "user123", Rol = "Usuario" }
        };

        // Valida las credenciales y retorna el usuario si son correctas, o null si no
        public Usuario? ValidarCredenciales(string nombreUsuario, string password)
        {
            return _usuarios.FirstOrDefault(u =>
                u.NombreUsuario.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
        }
    }
}