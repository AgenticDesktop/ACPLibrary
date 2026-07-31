using Agentic.ACPLibrary.Client;
using Agentic.ACPLibrary.Protocol;
using Agentic.ACPLibrary.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Agentic.ACPLibrary.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ACP client services into the DI container.
    /// </summary>
    public static IServiceCollection AddAcpClient(this IServiceCollection services)
    {
        services.AddTransient<IJsonRpcDispatcher, JsonRpcDispatcher>();
        services.AddTransient<IRequestTracker, RequestTracker>();
        services.AddSingleton<AcpClient>();
        services.AddSingleton<IAcpClient>(sp => sp.GetRequiredService<AcpClient>());
        return services;
    }
}
