using System.Security.Claims;
using HikariLegalSRL.Constants;
using Microsoft.AspNetCore.Authorization;

namespace HikariLegalSRL.Authorization
{
   // Sirve para poder usar el método TienePermiso en las vistas, para poder validar si un usuario tiene un permiso específico.(similar a lo que hace el atributo [Authorize] en los controladores)
    public static class AuthViewExtensions
    {
        public static async Task<bool> TienePermiso(
            this IAuthorizationService authorization, ClaimsPrincipal user, string codigo)
        {
            var resultado = await authorization.AuthorizeAsync(user, Permisos.Prefijo + codigo);
            return resultado.Succeeded;
        }
    }
}
