using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nova.Formats;

/// <summary>
/// Extension methods for configuring Nova.Formats services.
/// </summary>
public static class NovaFormatsExtensions
{
    /// <summary>
    /// Adds Nova.Formats services to the service collection.
    /// </summary>
    public static IServiceCollection AddNovaFormats(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentConverter, DocumentConverter>();
        services.AddSingleton<IPdfService, PdfService>();

        return services;
    }
}