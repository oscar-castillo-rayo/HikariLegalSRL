using HikariLegalSRL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HikariLegalSRL.Authorization
{
    // Proveedor de políticas dinámico. Evita tener que definir una política por cada permiso en el catálogo, y 
    // permite que el atributo [Permiso] funcione con cualquier código de permiso definido en PermisosCatalogo.
    public class PermisoPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermisoPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!policyName.StartsWith(Permisos.Prefijo, StringComparison.Ordinal))
                return await base.GetPolicyAsync(policyName);

            var codigo = policyName[Permisos.Prefijo.Length..];

            // Código que no existe en el catálogo (típicamente un typo en un atributo [Permiso("...")]):
            // se niega a todos, incluido el Administrador, para que el fallo sea visible.
            if (!PermisosCatalogo.Codigos().Contains(codigo))
            {
                return new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireAssertion(_ => false)
                    .Build();
            }

            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermisoRequirement(codigo))
                .Build();
        }
    }
}
