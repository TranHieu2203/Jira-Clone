using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BB.Web;

public static class WebExtensions
{
    public static IServiceCollection AddBbWeb(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static IApplicationBuilder UseBbTraceId(this IApplicationBuilder app) =>
        app.UseMiddleware<TraceIdMiddleware>();
}
