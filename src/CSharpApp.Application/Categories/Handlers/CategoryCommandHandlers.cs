using CSharpApp.Core.CQRS;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using CSharpApp.Core.Settings;
using CSharpApp.Application.Categories.Commands;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CSharpApp.Application.Categories.Handlers;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Category?>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Category?> Handle(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating category: {CategoryName}", command.Name);
            
            var createRequest = new CreateCategoryRequest
            {
                Name = command.Name,
                Image = command.Image
            };
            
            var category = await _apiClient.PostAsync<Category>(_restApiSettings.Categories!, createRequest);
            
            if (category == null)
            {
                _logger.LogWarning("Failed to create category: {CategoryName}", command.Name);
                return null;
            }

            _logger.LogInformation("Successfully created category {CategoryId}: {CategoryName}", category.Id, category.Name);
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category: {CategoryName}", command.Name);
            throw;
        }
    }
}

public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, Category?>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;

    public UpdateCategoryCommandHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<UpdateCategoryCommandHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Category?> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating category {CategoryId}: {CategoryName}", command.Id, command.Name);
            
            var updateRequest = new UpdateCategoryRequest
            {
                Name = command.Name,
                Image = command.Image
            };
            
            var endpoint = $"{_restApiSettings.Categories}/{command.Id}";
            var category = await _apiClient.PutAsync<Category>(endpoint, updateRequest);
            
            if (category == null)
            {
                _logger.LogWarning("Failed to update category {CategoryId}: {CategoryName}", command.Id, command.Name);
                return null;
            }

            _logger.LogInformation("Successfully updated category {CategoryId}: {CategoryName}", category.Id, category.Name);
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}: {CategoryName}", command.Id, command.Name);
            throw;
        }
    }
}

public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, bool>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;

    public DeleteCategoryCommandHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<DeleteCategoryCommandHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting category {CategoryId}", command.Id);
            
            var endpoint = $"{_restApiSettings.Categories}/{command.Id}";
            var success = await _apiClient.DeleteAsync(endpoint);
            
            if (success)
            {
                _logger.LogInformation("Successfully deleted category {CategoryId}", command.Id);
            }
            else
            {
                _logger.LogWarning("Failed to delete category {CategoryId}", command.Id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", command.Id);
            throw;
        }
    }
}