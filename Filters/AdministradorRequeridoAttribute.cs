using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SistemaReservasLaboratorios.Filters
{
    /// <summary>
    /// Restringe una acción a usuarios con rol Administrador.
    /// Si existe sesión pero el rol no es administrador, devuelve al Home.
    /// </summary>
    public class AdministradorRequeridoAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var nombreUsuario = context.HttpContext.Session.GetString("NombreUsuario");
            var rol = context.HttpContext.Session.GetString("Rol");

            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            if (!string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
