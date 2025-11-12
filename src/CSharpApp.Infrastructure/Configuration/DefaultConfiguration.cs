using CSharpApp.Infrastructure.Services;

namespace CSharpApp.Infrastructure.Configuration;

public static class DefaultConfiguration
{
    public static IServiceCollection AddDefaultConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RestApiSettings>(configuration.GetSection(nameof(RestApiSettings)));
        services.Configure<HttpClientSettings>(configuration.GetSection(nameof(HttpClientSettings)));

        // Register HTTP services
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IApiClient, ApiClient>();
        
        // Register application services
        services.AddScoped<IProductsService, ProductsService>();
        
        return services;
    }
}