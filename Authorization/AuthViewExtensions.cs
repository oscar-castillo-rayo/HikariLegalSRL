using System.Security.Claims;
using HikariLegalSRL.Constants;
using Microsoft.AspNetCore.Authorization;

namespace HikariLegalSRL.Authorization
{
   // Permite validar permisos en las vistas con TienePermiso.
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
