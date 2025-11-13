using CSharpApp.Core.CQRS;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Categories.Queries;

public class GetCategoriesQuery : IQuery<IReadOnlyCollection<Category>>
{
    // No parameters needed for getting all categories
}

public class GetCategoryByIdQuery : IQuery<Category?>
{
    public int Id { get; init; }

    public GetCategoryByIdQuery(int id)
    {
        Id = id;
    }
}