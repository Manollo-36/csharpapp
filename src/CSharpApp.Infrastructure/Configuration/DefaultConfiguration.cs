//using CSharpApp.Application.Categories;
using CSharpApp.Application.Categories;
using CSharpApp.Infrastructure.Services;
using CSharpApp.Core.Settings;
using CSharpApp.Core.Interfaces;
using CSharpApp.Core.CQRS;
using CSharpApp.Infrastructure.CQRS;
using CSharpApp.Application.Products.Handlers;
using CSharpApp.Application.Categories.Handlers;
using CSharpApp.Application.Products.Queries;
using CSharpApp.Application.Products.Commands;
using CSharpApp.Application.Categories.Queries;
using CSharpApp.Application.Categories.Commands;

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
        
        // Register CQRS services
        services.AddScoped<IMediator, Mediator>();
        
        // Register Product handlers
        services.AddScoped<IQueryHandler<GetProductsQuery, IReadOnlyCollection<Product>>, GetProductsQueryHandler>();
        services.AddScoped<IQueryHandler<GetProductByIdQuery, Product?>, GetProductByIdQueryHandler>();
        services.AddScoped<ICommandHandler<CreateProductCommand, Product?>, CreateProductCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateProductCommand, Product?>, UpdateProductCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteProductCommand, bool>, DeleteProductCommandHandler>();
        
        // Register Category handlers
        services.AddScoped<IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<Category>>, GetCategoriesQueryHandler>();
        services.AddScoped<IQueryHandler<GetCategoryByIdQuery, Category?>, GetCategoryByIdQueryHandler>();
        services.AddScoped<ICommandHandler<CreateCategoryCommand, Category?>, CreateCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateCategoryCommand, Category?>, UpdateCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteCategoryCommand, bool>, DeleteCategoryCommandHandler>();
        
        // Register legacy application services (for backward compatibility)
        services.AddScoped<IProductsService, ProductsService>();
        services.AddScoped<ICategoriesService, CategoriesService>();
        return services;
    }
}