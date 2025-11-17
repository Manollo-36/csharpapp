using Microsoft.Extensions.Options;
using CSharpApp.Core.CQRS;
using CSharpApp.Application.Products.Queries;
using CSharpApp.Application.Products.Commands;
using CSharpApp.Application.Categories.Queries;
using CSharpApp.Application.Categories.Commands;

var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Logging.ClearProviders().AddSerilog(logger);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDefaultConfiguration(builder.Configuration);
builder.Services.AddHttpConfiguration(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddApiVersioning();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Performance monitoring middleware (should be early in the pipeline)
app.UseMiddleware<CSharpApp.Infrastructure.Middleware.PerformanceLoggingMiddleware>();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    service = "CSharpApp.Api"
})).WithName("HealthCheck");

//app.UseHttpsRedirection();

var versionedEndpointRouteBuilder = app.NewVersionedApi();

// Products endpoints using CQRS
versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products", async (IMediator mediator) =>
    {
        var query = new GetProductsQuery();
        var products = await mediator.Send(query);
        return Results.Ok(products);
    })
    .WithName("GetProducts")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products/{id:int}", async (int id, IMediator mediator) =>
    {
        var query = new GetProductByIdQuery(id);
        var product = await mediator.Send(query);
        return product != null ? Results.Ok(product) : Results.NotFound($"Product with ID {id} not found");
    })
    .WithName("GetProductById")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/products", async (CreateProductRequest request, IMediator mediator) =>
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest("Product title is required");

        if (request.Price <= 0)
            return Results.BadRequest("Product price must be greater than 0");

        var command = new CreateProductCommand(request.Title, request.Price, request.Description, request.CategoryId, request.Images);
        var product = await mediator.Send(command);
        return product != null ? Results.Created($"api/v1.0/products/{product.Id}", product) : Results.BadRequest("Failed to create product");
    })
    .WithName("CreateProduct")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPut("api/v{version:apiVersion}/products/{id:int}", async (int id, UpdateProductRequest request, IMediator mediator) =>
    {
        var command = new UpdateProductCommand(
            id, 
            request.Title ?? string.Empty, 
            request.Price ?? 0, 
            request.Description ?? string.Empty, 
            request.CategoryId ?? 0, 
            request.Images ?? new List<string>());
        var product = await mediator.Send(command);
        return product != null ? Results.Ok(product) : Results.NotFound($"Product with ID {id} not found");
    })
    .WithName("UpdateProduct")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapDelete("api/v{version:apiVersion}/products/{id:int}", async (int id, IMediator mediator) =>
    {
        var command = new DeleteProductCommand(id);
        var success = await mediator.Send(command);
        return success ? Results.NoContent() : Results.NotFound($"Product with ID {id} not found");
    })
    .WithName("DeleteProduct")
    .HasApiVersion(1.0);

// Categories endpoints using CQRS
versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories", async (IMediator mediator) =>
    {
        var query = new GetCategoriesQuery();
        var categories = await mediator.Send(query);
        return Results.Ok(categories);
    })
    .WithName("GetCategories")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories/{id:int}", async (int id, IMediator mediator) =>
    {
        var query = new GetCategoryByIdQuery(id);
        var category = await mediator.Send(query);
        return category != null ? Results.Ok(category) : Results.NotFound($"Category with ID {id} not found");
    })
    .WithName("GetCategoryById")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/categories", async (CreateCategoryRequest request, IMediator mediator) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Category name is required");

        var command = new CreateCategoryCommand(request.Name, request.Image);
        var category = await mediator.Send(command);
        return category != null ? Results.Created($"api/v1.0/categories/{category.Id}", category) : Results.BadRequest("Failed to create category");
    })
    .WithName("CreateCategory")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPut("api/v{version:apiVersion}/categories/{id:int}", async (int id, UpdateCategoryRequest request, IMediator mediator) =>
    {
        var command = new UpdateCategoryCommand(id, request.Name ?? string.Empty, request.Image ?? string.Empty);
        var category = await mediator.Send(command);
        return category != null ? Results.Ok(category) : Results.NotFound($"Category with ID {id} not found");
    })
    .WithName("UpdateCategory")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapDelete("api/v{version:apiVersion}/categories/{id:int}", async (int id, IMediator mediator) =>
    {
        var command = new DeleteCategoryCommand(id);
        var success = await mediator.Send(command);
        return success ? Results.NoContent() : Results.NotFound($"Category with ID {id} not found");
    })
    .WithName("DeleteCategory")
    .HasApiVersion(1.0);

// Authentication status endpoint
versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/auth/status", async (IServiceProvider serviceProvider) =>
    {
        try
        {
            var settings = serviceProvider.GetRequiredService<IOptions<RestApiSettings>>().Value;
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            
            var httpClient = httpClientFactory.CreateClient("ExternalApi");
            
            // Test basic connectivity
            var authUrl = $"{httpClient.BaseAddress}{settings.Auth}";
            
            var authRequest = new { email = settings.Username, password = settings.Password };
            var json = System.Text.Json.JsonSerializer.Serialize(authRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(settings.Auth, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return Results.Ok(new {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                statusDescription = response.StatusCode.ToString(),
                authUrl = authUrl,
                baseUrl = httpClient.BaseAddress?.ToString(),
                authEndpoint = settings.Auth,
                username = settings.Username,
                responsePreview = responseContent.Length > 100 ? responseContent[..100] + "..." : responseContent,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Ok(new { 
                success = false, 
                error = ex.Message,
                type = ex.GetType().Name,
                timestamp = DateTime.UtcNow
            });
        }
    })
    .WithName("AuthStatus")
    .HasApiVersion(1.0);

// Performance monitoring endpoints
versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/performance/metrics", (IPerformanceMetricsService metricsService) =>
    {
        var statistics = metricsService.GetStatistics();
        return Results.Ok(statistics);
    })
    .WithName("GetPerformanceMetrics")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/performance/recent", (IPerformanceMetricsService metricsService, int? count) =>
    {
        var recentMetrics = metricsService.GetRecentMetrics(count ?? 50);
        return Results.Ok(recentMetrics);
    })
    .WithName("GetRecentMetrics")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/performance/slow", (IPerformanceMetricsService metricsService, int? thresholdMs) =>
    {
        var slowRequests = metricsService.GetSlowRequests(thresholdMs ?? 1000);
        return Results.Ok(slowRequests);
    })
    .WithName("GetSlowRequests")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapDelete("api/v{version:apiVersion}/performance/metrics", (IPerformanceMetricsService metricsService) =>
    {
        metricsService.ClearMetrics();
        return Results.Ok(new { message = "Performance metrics cleared", timestamp = DateTime.UtcNow });
    })
    .WithName("ClearPerformanceMetrics")
    .HasApiVersion(1.0);

app.Run();

// Make the Program class accessible for testing
public partial class Program { }