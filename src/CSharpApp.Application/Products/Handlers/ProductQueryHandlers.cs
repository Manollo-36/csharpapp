using CSharpApp.Core.CQRS;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using CSharpApp.Core.Settings;
using CSharpApp.Application.Products.Queries;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CSharpApp.Application.Products.Handlers;

public class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, IReadOnlyCollection<Product>>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<GetProductsQueryHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Product>> Handle(GetProductsQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching products from external API");
            
            var products = await _apiClient.GetAsync<List<Product>>(_restApiSettings.Products!);
            
            if (products == null || !products.Any())
            {
                _logger.LogWarning("No products returned from external API");
                return Array.Empty<Product>();
            }

            _logger.LogInformation("Successfully fetched {Count} products", products.Count);
            return products.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from external API");
            throw;
        }
    }
}

public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, Product?>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching product {ProductId} from external API", query.Id);
            
            var endpoint = $"{_restApiSettings.Products}/{query.Id}";
            var product = await _apiClient.GetAsync<Product>(endpoint);
            
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found in external API", query.Id);
                return null;
            }

            _logger.LogInformation("Successfully fetched product {ProductId}: {ProductTitle}", product.Id, product.Title);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} from external API", query.Id);
            throw;
        }
    }
}