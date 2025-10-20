namespace ProductData.Interfaces
{
    public interface IProductRepository : IGenericRepository<ProductEntities.Models.Product>
    {
        Task<IEnumerable<ProductEntities.Models.Product>> GetProductsWithCategoriesAsync();
    }
}