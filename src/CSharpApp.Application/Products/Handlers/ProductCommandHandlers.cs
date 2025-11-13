using CSharpApp.Core.CQRS;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using CSharpApp.Core.Settings;
using CSharpApp.Application.Products.Commands;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CSharpApp.Application.Products.Handlers;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Product?>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<CreateProductCommandHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Product?> Handle(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating product: {ProductTitle}", command.Title);
            
            var createRequest = new CreateProductRequest
            {
                Title = command.Title,
                Price = command.Price,
                Description = command.Description,
                CategoryId = command.CategoryId,
                Images = command.Images
            };
            
            var product = await _apiClient.PostAsync<Product>(_restApiSettings.Products!, createRequest);
            
            if (product == null)
            {
                _logger.LogWarning("Failed to create product: {ProductTitle}", command.Title);
                return null;
            }

            _logger.LogInformation("Successfully created product {ProductId}: {ProductTitle}", product.Id, product.Title);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductTitle}", command.Title);
            throw;
        }
    }
}

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Product?>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Product?> Handle(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating product {ProductId}: {ProductTitle}", command.Id, command.Title);
            
            var updateRequest = new UpdateProductRequest
            {
                Title = command.Title,
                Price = command.Price,
                Description = command.Description,
                CategoryId = command.CategoryId,
                Images = command.Images
            };
            
            var endpoint = $"{_restApiSettings.Products}/{command.Id}";
            var product = await _apiClient.PutAsync<Product>(endpoint, updateRequest);
            
            if (product == null)
            {
                _logger.LogWarning("Failed to update product {ProductId}: {ProductTitle}", command.Id, command.Title);
                return null;
            }

            _logger.LogInformation("Successfully updated product {ProductId}: {ProductTitle}", product.Id, product.Title);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}: {ProductTitle}", command.Id, command.Title);
            throw;
        }
    }
}

public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, bool>
{
    private readonly IApiClient _apiClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IApiClient apiClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _apiClient = apiClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteProductCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting product {ProductId}", command.Id);
            
            var endpoint = $"{_restApiSettings.Products}/{command.Id}";
            var success = await _apiClient.DeleteAsync(endpoint);
            
            if (success)
            {
                _logger.LogInformation("Successfully deleted product {ProductId}", command.Id);
            }
            else
            {
                _logger.LogWarning("Failed to delete product {ProductId}", command.Id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", command.Id);
            throw;
        }
    }
}