using ProductDTOs;

namespace ProductBusiness.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllAsync(string token);
        Task<ProductDto> GetByIdAsync(int id, string token);
        Task<ProductDto> CreateAsync(CreateProductDto dto, string token);
        Task<bool> UpdateAsync(int id, CreateProductDto dto);
        Task<bool> DeleteAsync(int id);
    }
}