using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BillingSuite.Infrastructure.Logging;

/// <summary>
/// Serilog configuration for BillingSuite application
/// Provides structured logging with multiple sinks (console, file)
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog for the application
    /// </summary>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var env = builder.Environment;
        var configuration = builder.Configuration;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "BillingSuite")
            .Enrich.WithProperty("Environment", env.EnvironmentName)
            .Enrich.WithProperty("Machine", Environment.MachineName)
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine("Logs", "app-.log"),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true)
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(le => le.Level >= LogEventLevel.Error)
                .WriteTo.File(
                    path: Path.Combine("Logs", "errors", "error-.log"),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    shared: true))
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}
