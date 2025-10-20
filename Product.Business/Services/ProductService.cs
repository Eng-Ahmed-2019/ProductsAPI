using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Newtonsoft.Json;
using ProductBusiness.Interfaces;
using ProductData.Interfaces;
using ProductDTOs;
using ProductEntities.Models;
using System.Net.Http.Headers;

namespace ProductBusiness.Services
{
    public class ProductService : IProductService
    {
        private readonly HttpClient _httpClient;
        private readonly IProductRepository _productRepo;

        public ProductService(IProductRepository repository, HttpClient httpClient)
        {
            _productRepo = repository;
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync(string token)
        {
            var products = await _productRepo.GetProductsWithCategoriesAsync();
            var productDtos = new List<ProductDto>();

            foreach (var product in products)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7286/api/categories/{product.CategoryId}");
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                var categoryName = string.Empty;

                if (response.IsSuccessStatusCode)
                {
                    var categoryJson = await response.Content.ReadAsStringAsync();
                    var category = JsonConvert.DeserializeObject<CategoryDTO>(categoryJson);
                    categoryName = category?.Name ?? "";
                }

                productDtos.Add(new ProductDto
                {
                    Id = product.Id,
                    CategoryId = product.CategoryId,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageUrl = product.ImageUrl,
                    CategoryName = categoryName
                });
            }

            return productDtos;
        }

        public async Task<ProductDto?> GetByIdAsync(int id, string token)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
                return null;

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7286/api/categories/{product.CategoryId}");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            string categoryName = string.Empty;
            if (response.IsSuccessStatusCode)
            {
                var categoryJson = await response.Content.ReadAsStringAsync();
                var category = JsonConvert.DeserializeObject<CategoryDTO>(categoryJson);
                categoryName = category?.Name ?? string.Empty;
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                CategoryName = categoryName
            };

            return productDto;
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto, string token)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId
            };

            await _productRepo.AddAsync(product);
            await _productRepo.SaveChangesAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7286/api/categories/{dto.CategoryId}");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            string categoryName = string.Empty;

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var category = JsonConvert.DeserializeObject<CategoryDTO>(json);
                categoryName = category?.Name ?? "";
            }

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                CategoryName = categoryName
            };
        }

        public async Task<bool> UpdateAsync(int id, CreateProductDto dto)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
                return false;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.ImageUrl = dto.ImageUrl;
            product.CategoryId = dto.CategoryId;

            _productRepo.Update(product);
            return await _productRepo.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return false;

            _productRepo.Delete(product);
            return await _productRepo.SaveChangesAsync();
        }
    }
}