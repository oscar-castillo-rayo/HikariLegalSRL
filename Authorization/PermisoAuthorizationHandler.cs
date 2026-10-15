using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace HikariLegalSRL.Authorization
{
   // Permite validar si un usuario tiene un permiso específico, usando el servicio IPermisoEvaluador para verificar los permisos del usuario.
    public class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
    {
        private readonly IPermisoEvaluador _evaluador;

        public PermisoAuthorizationHandler(IPermisoEvaluador evaluador) => _evaluador = evaluador;

        //Esta funcion sobrescribe el metodo HandleRequirementAsync de la clase AuthorizationHandler, que se encarga de manejar la autorización para un requisito específico (PermisoRequirement).
        //El método verifica si el usuario está autenticado y si tiene el permiso requerido. Si el usuario tiene el permiso, se llama a context.Succeed(requirement) para indicar que la autorización fue exitosa.
        
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
