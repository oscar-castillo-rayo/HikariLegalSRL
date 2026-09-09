using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace HikariLegalSRL.Authorization
{
   // Permite validar si un usuario tiene un permiso específico, usando el servicio IPermisoEvaluador para verificar los permisos del usuario.
    public class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
    {
        private readonly IPermisoEvaluador _evaluador;

        public PermisoAuthorizationHandler(IPermisoEvaluador evaluador) => _evaluador = evaluador;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermisoRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return;

            if (await _evaluador.TienePermisoAsync(context.User, requirement.Codigo))
                context.Succeed(requirement);
        }
    }
}
