
namespace SistemaReservasLaboratorios.Services
{
    public static class ReglasLogin
    {
        public static string? ValidarCampos(
            string? nombreUsuario,
            string? password)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) ||
                string.IsNullOrWhiteSpace(password))
            {
                return "Debe ingresar usuario y contraseña.";
            }

            if (nombreUsuario.Trim().Length > 50)
            {
                return "El nombre de usuario no puede superar los 50 caracteres.";
            }

            if (password.Length > 100)
            {
                return "La contraseña no puede superar los 100 caracteres.";
            }

            return null;
        }

        public static string NormalizarUsuario(string nombreUsuario)
        {
            return nombreUsuario.Trim();
        }
    }
}
