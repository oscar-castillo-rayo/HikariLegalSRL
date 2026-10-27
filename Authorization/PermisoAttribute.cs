using HikariLegalSRL.Constants;
using Microsoft.AspNetCore.Authorization;

namespace HikariLegalSRL.Authorization
{
    // Como [Authorize], pero exige un permiso específico.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class PermisoAttribute : AuthorizeAttribute
    {
        //Concatena el prefijo con el código del permiso para formar el 
        // nombre de la política que se usará en la autorización. 
        public PermisoAttribute(string codigo) : base(Permisos.Prefijo + codigo)
        {
        }
    }
}
