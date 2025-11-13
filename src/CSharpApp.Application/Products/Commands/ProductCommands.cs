using CSharpApp.Core.CQRS;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Products.Commands;

public class CreateProductCommand : ICommand<Product?>
{
    public string Title { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Description { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public List<string> Images { get; init; } = new();

    public CreateProductCommand(string title, decimal price, string description, int categoryId, List<string> images)
    {
        Title = title;
        Price = price;
        Description = description;
        CategoryId = categoryId;
        Images = images;
    }
}

public class UpdateProductCommand : ICommand<Product?>
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Description { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public List<string> Images { get; init; } = new();

    public UpdateProductCommand(int id, string title, decimal price, string description, int categoryId, List<string> images)
    {
        Id = id;
        Title = title;
        Price = price;
        Description = description;
        CategoryId = categoryId;
        Images = images;
    }
}

public class DeleteProductCommand : ICommand<bool>
{
    public int Id { get; init; }

    public DeleteProductCommand(int id)
    {
        Id = id;
    }
}