using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SistemaReservasLaboratorios.Filters
{
    // Filtro reutilizable: exige que exista "NombreUsuario" en la sesión.
    // Si no hay sesión, corta la acción y redirige al login.
    public class SesionRequeridaAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // La respuesta depende del usuario en sesión: no se guarda en caché
            context.HttpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.HttpContext.Response.Headers.Pragma = "no-cache";
            context.HttpContext.Response.Headers.Expires = "0";

            var nombreUsuario = context.HttpContext.Session.GetString("NombreUsuario");

            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
            }
        }
    }
}
