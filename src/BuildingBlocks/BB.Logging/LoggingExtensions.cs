using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BB.Logging;

public static class LoggingExtensions
{
    public static IHostBuilder UseBbSerilog(this IHostBuilder host)
    {
        return host.UseSerilog((ctx, services, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration)
               .ReadFrom.Services(services)
               .Enrich.FromLogContext()
               .Enrich.WithProperty("Application", ctx.HostingEnvironment.ApplicationName)
               .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
               .WriteTo.Console(outputTemplate:
                   "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId}] {Message:lj} {Properties:j}{NewLine}{Exception}")
               .WriteTo.File(
                   path: "logs/app-.log",
                   rollingInterval: RollingInterval.Day,
                   retainedFileCountLimit: 14,
                   outputTemplate:
                   "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}");
        });
    }
}
