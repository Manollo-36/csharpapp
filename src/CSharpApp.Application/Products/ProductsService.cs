namespace CSharpApp.Application.Products;

public class ProductsService : IProductsService
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<ProductsService> _logger;

    public ProductsService(IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings, 
        ILogger<ProductsService> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Product>> GetProducts()
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
            _logger.LogError(ex, "Failed to fetch products from external API");
            throw;
        }
    }

    public async Task<Product?> GetProductById(int id)
    {
        try
        {
            _logger.LogInformation("Fetching product {ProductId} from external API", id);
            
            var endpoint = $"{_restApiSettings.Products}/{id}";
            var product = await _apiClient.GetAsync<Product>(endpoint);
            
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found", id);
                return null;
            }

            _logger.LogInformation("Successfully fetched product {ProductId}: {Title}", id, product.Title);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch product {ProductId} from external API", id);
            throw;
        }
    }

    public async Task<Product?> CreateProduct(CreateProductRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new product: {Title}", request.Title);
            
            var product = await _apiClient.PostAsync<Product>(_restApiSettings.Products!, request);
            
            if (product == null)
            {
                _logger.LogWarning("Failed to create product: {Title}", request.Title);
                return null;
            }

            _logger.LogInformation("Successfully created product {ProductId}: {Title}", product.Id, product.Title);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create product: {Title}", request.Title);
            throw;
        }
    }

    public async Task<Product?> UpdateProduct(int id, UpdateProductRequest request)
    {
        try
        {
            _logger.LogInformation("Updating product {ProductId}", id);
            
            var endpoint = $"{_restApiSettings.Products}/{id}";
            var product = await _apiClient.PutAsync<Product>(endpoint, request);
            
            if (product == null)
            {
                _logger.LogWarning("Failed to update product {ProductId}", id);
                return null;
            }

            _logger.LogInformation("Successfully updated product {ProductId}: {Title}", id, product.Title);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteProduct(int id)
    {
        try
        {
            _logger.LogInformation("Deleting product {ProductId}", id);
            
            var endpoint = $"{_restApiSettings.Products}/{id}";
            var success = await _apiClient.DeleteAsync(endpoint);
            
            if (success)
            {
                _logger.LogInformation("Successfully deleted product {ProductId}", id);
            }
            else
            {
                _logger.LogWarning("Failed to delete product {ProductId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete product {ProductId}", id);
            throw;
        }
    }
}