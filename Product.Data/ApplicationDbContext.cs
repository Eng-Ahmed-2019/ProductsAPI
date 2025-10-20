using ProductEntities.Models;
using Microsoft.EntityFrameworkCore;

namespace ProductData
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public ApplicationDbContext()
        {
        }

        public DbSet<Product> Products { get; set; }
    }
}