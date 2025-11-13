using CSharpApp.Core.CQRS;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Categories.Commands;

public class CreateCategoryCommand : ICommand<Category?>
{
    public string Name { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;

    public CreateCategoryCommand(string name, string image)
    {
        Name = name;
        Image = image;
    }
}

public class UpdateCategoryCommand : ICommand<Category?>
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;

    public UpdateCategoryCommand(int id, string name, string image)
    {
        Id = id;
        Name = name;
        Image = image;
    }
}

public class DeleteCategoryCommand : ICommand<bool>
{
    public int Id { get; init; }

    public DeleteCategoryCommand(int id)
    {
        Id = id;
    }
}