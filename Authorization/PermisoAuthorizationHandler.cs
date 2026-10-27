using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace HikariLegalSRL.Authorization
{
   // Evalúa el requisito de permiso con IPermisoEvaluador.
    public class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
    {
        private readonly IPermisoEvaluador _evaluador;

        public PermisoAuthorizationHandler(IPermisoEvaluador evaluador) => _evaluador = evaluador;

        // Autoriza si el usuario está autenticado y tiene el permiso requerido.
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
