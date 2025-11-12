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

app.Run();