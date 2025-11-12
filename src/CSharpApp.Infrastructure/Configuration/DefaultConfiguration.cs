//using CSharpApp.Application.Categories;
using CSharpApp.Application.Categories;
using CSharpApp.Infrastructure.Services;
using CSharpApp.Core.Settings;
using CSharpApp.Core.Interfaces;

namespace CSharpApp.Infrastructure.Configuration;

public static class DefaultConfiguration
{
    public static IServiceCollection AddDefaultConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RestApiSettings>(configuration.GetSection(nameof(RestApiSettings)));
        services.Configure<HttpClientSettings>(configuration.GetSection(nameof(HttpClientSettings)));
        services.Configure<PerformanceSettings>(configuration.GetSection(PerformanceSettings.SectionName));

        // Register HTTP services
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IApiClient, ApiClient>();
        services.AddScoped<AuthenticationTestService>();
        //services.AddScoped<SimpleAuthenticationTest>();
        
        // Register performance services
        services.AddSingleton<IPerformanceMetricsService, PerformanceMetricsService>();
        
        // Register application services
        services.AddScoped<IProductsService, ProductsService>();
        services.AddScoped<ICategoriesService, CategoriesService>();
        return services;
    }
}