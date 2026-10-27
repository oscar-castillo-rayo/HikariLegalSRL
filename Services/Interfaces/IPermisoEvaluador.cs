using System.Security.Claims;
using HikariLegalSRL.Models;

namespace HikariLegalSRL.Services.Interfaces
{
    /// <summary>
    /// Define la evaluación de los permisos de una persona usuaria a partir de los de sus roles, la búsqueda de quienes tienen un permiso y la invalidación de la caché de un rol.
    /// </summary>
    public interface IPermisoEvaluador
    {
        Task<bool> TienePermisoAsync(ClaimsPrincipal user, string codigo);

        Task<bool> TienePermisoAsync(ApplicationUser usuario, string codigo);

        Task<ISet<string>> PermisosDeUsuarioAsync(ClaimsPrincipal user);

        Task<List<ApplicationUser>> UsuariosActivosConPermisoAsync(string codigo);

        void InvalidarRol(string rolId);
    }
}
