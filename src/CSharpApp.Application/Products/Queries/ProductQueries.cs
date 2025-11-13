using CSharpApp.Core.CQRS;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Products.Queries;

public class GetProductsQuery : IQuery<IReadOnlyCollection<Product>>
{
    // No parameters needed for getting all products
}

public class GetProductByIdQuery : IQuery<Product?>
{
    public int Id { get; init; }

    public GetProductByIdQuery(int id)
    {
        Id = id;
    }
}