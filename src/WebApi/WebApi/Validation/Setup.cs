namespace MikeAllan.VerticalSliceArchitecture.Validation;

/// <summary>
/// Request validation: the built-in Minimal API validation reads DataAnnotations and
/// <see cref="System.ComponentModel.DataAnnotations.IValidatableObject"/> on endpoint parameters and
/// request types (including nested collection items) and answers a failure with a 400
/// <c>HttpValidationProblemDetails</c> before the handler runs.
/// </summary>
public static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddRequestValidation()
        {
            services.AddValidation();

            return services;
        }
    }
}
