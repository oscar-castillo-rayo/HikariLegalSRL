using Microsoft.AspNetCore.Identity;

namespace HikariLegalSRL.Exceptions
{
    public class TransaccionFallidaException : Exception
    {
        public IdentityResult Resultado { get; }

        public TransaccionFallidaException(IdentityResult resultado) : base("Una operación de Identity no se pudo completar.")
        {
            Resultado = resultado;
        }

        public TransaccionFallidaException(string descripcion)
            : this(IdentityResult.Failed(new IdentityError { Description = descripcion }))
        {
        }

        public static void Exigir(IdentityResult resultado)
        {
            if (!resultado.Succeeded)
                throw new TransaccionFallidaException(resultado);
        }
    }
}
