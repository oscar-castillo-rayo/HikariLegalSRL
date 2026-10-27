using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HikariLegalSRL.ModelBinding
{
    // Los input number envían el decimal con punto; se parsea con cultura invariante para
    // que funcione aunque la cultura del sistema use coma (es-CR).
    public class InvariantDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;
            var isNullableType = Nullable.GetUnderlyingType(bindingContext.ModelType) is not null;

            if (string.IsNullOrEmpty(value))
            {
                if (isNullableType)
                    bindingContext.Result = ModelBindingResult.Success(null);

                return Task.CompletedTask;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                bindingContext.Result = ModelBindingResult.Success(parsed);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    $"El valor '{value}' no es un número válido para {bindingContext.ModelMetadata.GetDisplayName()}.");
            }

            return Task.CompletedTask;
        }
    }

    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var modelType = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;

            return modelType == typeof(decimal) ? new InvariantDecimalModelBinder() : null;
        }
    }
}
