namespace CSharpApp.Core.Interfaces;

public interface IProductsService
{
    Task<IReadOnlyCollection<Product>> GetProducts();
    Task<Product?> GetProductById(int id);
    Task<Product?> CreateProduct(CreateProductRequest request);
    Task<Product?> UpdateProduct(int id, UpdateProductRequest request);
    Task<bool> DeleteProduct(int id);
}