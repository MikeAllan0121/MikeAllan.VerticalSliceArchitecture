using System.Text.Json.Serialization;

namespace MikeAllan.VerticalSliceArchitecture.JsonSerialization;

/// <summary>
/// How the API reads and writes JSON, beyond the web defaults (camelCase, case-insensitive on read).
/// </summary>
/// <remarks>
/// Enums travel as their names (<c>"Issued"</c>, not <c>2</c>), so a client never has to know the numbers
/// and reordering an enum can't silently change the contract. The OpenAPI document reads the same options,
/// so it lists the names too.
/// </remarks>
public static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJsonSerialization()
        {
            services.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            return services;
        }
    }
}
