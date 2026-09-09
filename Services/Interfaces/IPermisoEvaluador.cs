using System.Security.Claims;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IPermisoEvaluador
    {
        Task<bool> TienePermisoAsync(ClaimsPrincipal user, string codigo);

        Task<ISet<string>> PermisosDeUsuarioAsync(ClaimsPrincipal user);

        void InvalidarRol(string rolId);
    }
}
