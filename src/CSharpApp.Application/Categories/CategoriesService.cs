namespace CSharpApp.Application.Categories;

public class CategoriesService : ICategoriesService
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<CategoriesService> _logger;

    public CategoriesService(IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings, 
        ILogger<CategoriesService> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Category>> GetCategories()
    {
        try
        {
            _logger.LogInformation("Fetching categories from external API");

            var categories = await _apiClient.GetAsync<List<Category>>(_restApiSettings.Categories!);

            if (categories == null || !categories.Any())
            {
                _logger.LogWarning("No categories returned from external API");
                return Array.Empty<Category>();
            }

            _logger.LogInformation("Successfully fetched {Count} categories", categories.Count);
            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch categories from external API");
            throw;
        }
    }

    public async Task<Category?> GetCategoryById(int id)
    {
        try
        {
            _logger.LogInformation("Fetching category {CategoryId} from external API", id);

            var endpoint = $"{_restApiSettings.Categories}/{id}";
            var category = await _apiClient.GetAsync<Category>(endpoint);

            if (category == null)
            {
                _logger.LogWarning("Category {CategoryId} not found", id);
                return null;
            }

            _logger.LogInformation("Successfully fetched category {CategoryId}: {Name}", id, category.Name);
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch category {CategoryId} from external API", id);
            throw;
        }
    }

    public async Task<Category?> CreateCategory(CreateCategoryRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new category: {Name}", request.Name);

            var category = await _apiClient.PostAsync<Category>(_restApiSettings.Categories!, request);

            if (category == null)
            {
                _logger.LogWarning("Failed to create category: {Name}", request.Name);
                return null;
            }

            _logger.LogInformation("Successfully created category {CategoryId}: {Name}", category.Id, category.Name);
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create category: {Name}", request.Name);
            throw;
        }
    }

    public async Task<Category?> UpdateCategory(int id, UpdateCategoryRequest request)
    {
        try
        {
            _logger.LogInformation("Updating category {CategoryId}", id);

            var endpoint = $"{_restApiSettings.Categories}/{id}";
            var category = await _apiClient.PutAsync<Category>(endpoint, request);

            if (category == null)
            {
                _logger.LogWarning("Failed to update category {CategoryId}", id);
                return null;
            }

            _logger.LogInformation("Successfully updated category {CategoryId}: {Name}", id, category.Name);
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update category {CategoryId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCategory(int id)
    {
        try
        {
            _logger.LogInformation("Deleting category {CategoryId}", id);

            var endpoint = $"{_restApiSettings.Categories}/{id}";
            var success = await _apiClient.DeleteAsync(endpoint);
            
            if (success)
            {
                _logger.LogInformation("Successfully deleted category {CategoryId}", id);
            }
            else
            {
                _logger.LogWarning("Failed to delete category {CategoryId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete category {CategoryId}", id);
            throw;
        }
    }
}