using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaReservasLaboratorios.Data;
using SistemaReservasLaboratorios.Models;

namespace SistemaReservasLaboratorios.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Usuario> _hasher;

        public AuthService(AppDbContext context, IPasswordHasher<Usuario> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        // Busca el usuario por nombre y verifica la contraseña contra el hash guardado
        public Usuario? ValidarCredenciales(string nombreUsuario, string password)
        {
            // Se consulta solo por nombre; la contraseña se valida en memoria con el hasher
            var usuario = _context.Usuarios.FirstOrDefault(u =>
                u.NombreUsuario.ToLower() == nombreUsuario.ToLower());

            if (usuario == null)
            {
                return null;
            }

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);

            // Se acepta un hash válido y también cuando conviene regenerarlo
            if (resultado == PasswordVerificationResult.Success ||
                resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return usuario;
            }

            return null;
        }
    }
}
