using CSharpApp.Core.CQRS;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using CSharpApp.Core.Settings;
using CSharpApp.Application.Categories.Queries;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CSharpApp.Application.Categories.Handlers;

public class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<Category>>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;

    public GetCategoriesQueryHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<GetCategoriesQueryHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Category>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken = default)
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
            _logger.LogError(ex, "Error fetching categories from external API");
            throw;
        }
    }
}

public class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, Category?>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

    public GetCategoryByIdQueryHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<GetCategoryByIdQueryHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Category?> Handle(GetCategoryByIdQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching category {CategoryId} from external API", query.Id);
            
            var endpoint = $"{_restApiSettings.Categories}/{query.Id}";
            var category = await _apiClient.GetAsync<Category>(endpoint);
            
            if (category == null)
            {
                _logger.LogWarning("Category {CategoryId} not found in external API", query.Id);
                return null;
            }

            _logger.LogInformation("Successfully fetched category {CategoryId}: {CategoryName}", category.Id, category.Name);
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category {CategoryId} from external API", query.Id);
            throw;
        }
    }
}