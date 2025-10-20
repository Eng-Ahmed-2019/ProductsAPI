using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductEntities.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { set; get; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(50)]
        public string Name { set; get; } = string.Empty;

        [StringLength(200, ErrorMessage = "Number of characters must be less than or equal to 200")]
        public string? Description { set; get; }

        [Required(ErrorMessage = "Price is required")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { set; get; }

        [Required(ErrorMessage = "Stock is required")]
        public int Stock { set; get; }

        public string? ImageUrl { set; get; }

        public int CategoryId { set; get; }
    }
}