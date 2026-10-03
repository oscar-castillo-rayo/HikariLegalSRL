using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace HikariLegalSRL.Extensions
{
    public static class TransaccionServiceExtensions
    {
        public static async Task<IdentityResult> EjecutarIdentityAsync(this ITransaccionService transaccionService, Func<Task> pasos)
        {
            try
            {
                await transaccionService.EjecutarAsync(pasos);
                return IdentityResult.Success;
            }
            catch (TransaccionFallidaException ex)
            {
                return ex.Resultado;
            }
        }
    }
}
