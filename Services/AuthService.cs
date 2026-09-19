using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        // Valida las credenciales y retorna el usuario si son correctas, o null si no
        public Usuario? ValidarCredenciales(string nombreUsuario, string password)
        {
            return _context.Usuarios.FirstOrDefault(u =>
                u.NombreUsuario.ToLower() == nombreUsuario.ToLower() &&
                u.Password == password);
        }
    }
}