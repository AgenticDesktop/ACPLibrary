using Agentic.ACPLibrary.Agent;
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

    /// <summary>
    /// Registers ACP agent services into the DI container.
    /// Note: a process should register either the ACP client or the ACP agent, not both.
    /// </summary>
    public static IServiceCollection AddAcpAgent<THandler>(this IServiceCollection services)
        where THandler : class, IAcpAgentHandler
    {
        services.AddTransient<IAgentTransport, StdioHostTransport>();
        services.AddTransient<IJsonRpcDispatcher, JsonRpcDispatcher>();
        services.AddTransient<IRequestTracker, RequestTracker>();
        services.AddSingleton<IAcpAgentHandler, THandler>();
        services.AddSingleton<AcpAgent>();
        services.AddSingleton<IAcpAgent>(sp => sp.GetRequiredService<AcpAgent>());
        return services;
    }
}
