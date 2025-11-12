namespace CSharpApp.Core.Interfaces;

public interface ICategoriesService
{
    Task<IReadOnlyCollection<Category>> GetCategories();
    Task<Category?> GetCategoryById(int id);
    Task<Category?> CreateCategory(CreateCategoryRequest request);
    Task<Category?> UpdateCategory(int id, UpdateCategoryRequest request);
    Task<bool> DeleteCategory(int id);
}