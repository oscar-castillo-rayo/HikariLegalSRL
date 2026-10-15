using System.Security.Claims;
using HikariLegalSRL.Models;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IPermisoEvaluador
    {
        Task<bool> TienePermisoAsync(ClaimsPrincipal user, string codigo);

        Task<bool> TienePermisoAsync(ApplicationUser usuario, string codigo);

        Task<ISet<string>> PermisosDeUsuarioAsync(ClaimsPrincipal user);

        Task<List<ApplicationUser>> UsuariosActivosConPermisoAsync(string codigo);

        void InvalidarRol(string rolId);
    }
}
