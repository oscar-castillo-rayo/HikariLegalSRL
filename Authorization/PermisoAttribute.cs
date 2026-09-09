using HikariLegalSRL.Constants;
using Microsoft.AspNetCore.Authorization;

namespace HikariLegalSRL.Authorization
{
    // Sirve para poder usar el atributo [Permiso] en los controladores, para poder validar si un usuario tiene un permiso específico.
    // (similar a lo que hace el atributo [Authorize] en los controladores)
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
