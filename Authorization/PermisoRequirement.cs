using Microsoft.AspNetCore.Authorization;

namespace HikariLegalSRL.Authorization
{
    // Requisito de autorización que exige que el usuario (a través de su rol) tenga un permiso concreto.
    // El `Codigo` es un valor del catálogo, p. ej. "roles.crear".
    public class PermisoRequirement : IAuthorizationRequirement
    {
        public string Codigo { get; }

        public PermisoRequirement(string codigo) => Codigo = codigo;
    }
}
