using ProductData.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ProductData.Repositories
{
    public class ProductRepository : GenericRepository<ProductEntities.Models.Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductEntities.Models.Product>> GetProductsWithCategoriesAsync()
        {
            return await _context.Products.ToListAsync();
        }
    }
}