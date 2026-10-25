using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace HikariLegalSRL.ModelBinding
{
    public static class MensajesModelBindingEspanol
    {
        public static void Aplicar(DefaultModelBindingMessageProvider mensajes)
        {
            mensajes.SetValueMustNotBeNullAccessor(_ => "Este campo es obligatorio.");
            mensajes.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor de {campo}.");
            mensajes.SetMissingKeyOrValueAccessor(() => "Se requiere un valor para este campo.");
            mensajes.SetMissingRequestBodyRequiredValueAccessor(() => "Se requiere un cuerpo de solicitud.");
            mensajes.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
            mensajes.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
            mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
            mensajes.SetUnknownValueIsInvalidAccessor(campo => $"El valor ingresado no es válido para {campo}.");
            mensajes.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
            mensajes.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor ingresado no es válido.");
            mensajes.SetNonPropertyValueMustBeANumberAccessor(() => "El campo debe ser un número.");
        }
    }
}
