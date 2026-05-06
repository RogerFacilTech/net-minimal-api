using System.Reflection;

namespace FacShopAPI.Shared.Web;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpointsFromAssembly(
        this IServiceCollection services, Assembly assembly)
    {
        IEnumerable<Type> endpointTypes;
        try
        {
            endpointTypes = assembly
                .GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && t.IsAssignableTo(typeof(IEndpoint)));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to scan assembly '{assembly.FullName}' for IEndpoint implementations.", ex);
        }

        foreach (var type in endpointTypes)
            services.AddTransient(typeof(IEndpoint), type);

        return services;
    }

    public static WebApplication MapRegisteredEndpoints(this WebApplication app)
    {
        var endpoints = app.Services.GetServices<IEndpoint>();
        foreach (var endpoint in endpoints)
            endpoint.MapEndpoints(app);

        return app;
    }
}
