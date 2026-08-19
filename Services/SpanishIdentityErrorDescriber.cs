using Microsoft.AspNetCore.Identity;

namespace HikariLegalSRL.Services
{
    public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateEmail(string email) => new()
        {
            Code = nameof(DuplicateEmail),
            Description = $"El correo electrónico '{email}' ya está registrado"
        };

        public override IdentityError DuplicateUserName(string userName) => new()
        {
            Code = nameof(DuplicateUserName),
            Description = $"El correo electrónico '{userName}' ya está en uso"
        };

        public override IdentityError PasswordTooShort(int length) => new()
        {
            Code = nameof(PasswordTooShort),
            Description = $"La contraseña debe tener al menos'{length}' caracteres"
        };

        public override IdentityError PasswordRequiresNonAlphanumeric() => new()
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = $"La contraseña debe incluir al menos un caracter especial."
        };
        public override IdentityError PasswordRequiresDigit() => new()
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "La contraseña debe incluir al menos un número."
        };
        public override IdentityError PasswordRequiresLower() => new()
        {
            Code = nameof(PasswordRequiresLower),
            Description = "La contraseña debe incluir al menos una minúscula."
        };

        public override IdentityError PasswordRequiresUpper() => new()
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "La contraseña debe incluir al menos una mayúscula."
        };

        public override IdentityError InvalidEmail(string email) => new()
        {
            Code = nameof(InvalidEmail),
            Description = $"El correo electrónico '{email}' no es válido"
        };

    }
}
