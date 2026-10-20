using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Ports;

namespace Hosts.Base;

/// <summary>
/// Shared startup for all Hosts.* (design G4/D1'): cross-cutting registration plus the middleware
/// pipeline; the binding adapters for SharedKernel cross-cutting ports (<c>IClock</c>, <c>IEventBus</c>)
/// live here — never in one module's Infrastructure.
/// 橫切註冊與中介層管線集中於此；SharedKernel port binding adapter 只住 HostBase。
/// </summary>
public static class HostBaseComposition
{
    public static IServiceCollection AddHostBase(this IServiceCollection services)
    {
        services.AddScoped<TenantScope>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        return services;
    }

    public static IApplicationBuilder UseHostBasePipeline(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantScopeMiddleware>();
        return app;
    }
}
