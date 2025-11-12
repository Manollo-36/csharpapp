using Microsoft.Extensions.Options;

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

//app.UseHttpsRedirection();

var versionedEndpointRouteBuilder = app.NewVersionedApi();

// Products endpoints
versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products", async (IProductsService productsService) =>
    {
        var products = await productsService.GetProducts();
        return Results.Ok(products);
    })
    .WithName("GetProducts")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products/{id:int}", async (int id, IProductsService productsService) =>
    {
        var product = await productsService.GetProductById(id);
        return product != null ? Results.Ok(product) : Results.NotFound($"Product with ID {id} not found");
    })
    .WithName("GetProductById")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/products", async (CreateProductRequest request, IProductsService productsService) =>
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest("Product title is required");

        if (request.Price <= 0)
            return Results.BadRequest("Product price must be greater than 0");

        var product = await productsService.CreateProduct(request);
        return product != null ? Results.Created($"api/v1.0/products/{product.Id}", product) : Results.BadRequest("Failed to create product");
    })
    .WithName("CreateProduct")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPut("api/v{version:apiVersion}/products/{id:int}", async (int id, UpdateProductRequest request, IProductsService productsService) =>
    {
        var product = await productsService.UpdateProduct(id, request);
        return product != null ? Results.Ok(product) : Results.NotFound($"Product with ID {id} not found");
    })
    .WithName("UpdateProduct")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapDelete("api/v{version:apiVersion}/products/{id:int}", async (int id, IProductsService productsService) =>
    {
        var success = await productsService.DeleteProduct(id);
        return success ? Results.NoContent() : Results.NotFound($"Product with ID {id} not found");
    })
    .WithName("DeleteProduct")
    .HasApiVersion(1.0);

// Categories endpoints
versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories", async (ICategoriesService categoriesService) =>
    {
        var categories = await categoriesService.GetCategories();
        return Results.Ok(categories);
    })
    .WithName("GetCategories")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories/{id:int}", async (int id, ICategoriesService categoriesService) =>
    {
        var category = await categoriesService.GetCategoryById(id);
        return category != null ? Results.Ok(category) : Results.NotFound($"Category with ID {id} not found");
    })
    .WithName("GetCategoryById")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/categories", async (CreateCategoryRequest request, ICategoriesService categoriesService) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Category name is required");

        var category = await categoriesService.CreateCategory(request);
        return category != null ? Results.Created($"api/v1.0/categories/{category.Id}", category) : Results.BadRequest("Failed to create category");
    })
    .WithName("CreateCategory")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPut("api/v{version:apiVersion}/categories/{id:int}", async (int id, UpdateCategoryRequest request, ICategoriesService categoriesService) =>
    {
        var category = await categoriesService.UpdateCategory(id, request);
        return category != null ? Results.Ok(category) : Results.NotFound($"Category with ID {id} not found");
    })
    .WithName("UpdateCategory")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapDelete("api/v{version:apiVersion}/categories/{id:int}", async (int id, ICategoriesService categoriesService) =>
    {
        var success = await categoriesService.DeleteCategory(id);
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