using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.File;

namespace Nova.Core;

/// <summary>
/// Extension methods for configuring Nova.Core services.
/// </summary>
public static class NovaCoreExtensions
{
    /// <summary>
    /// Adds Nova.Core services to the service collection.
    /// </summary>
    public static IServiceCollection AddNovaCore(this IServiceCollection services, string appName, string? logDirectory = null)
    {
        // Configure Serilog
        var logPath = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaOffice",
            "Logs",
            $"{appName}-.log");

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Core services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<IAutosaveService, AutosaveService>();
        services.AddSingleton<ICrashRecoveryService, CrashRecoveryService>();
        services.AddSingleton<IUndoRedoService, UndoRedoService>();

        return services;
    }
}