using ProductDTOs;
using Microsoft.AspNetCore.Mvc;
using ProductBusiness.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ProductPresentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var products = await _productService.GetAllAsync(token);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var product = await _productService.GetByIdAsync(id, token);
            if (product == null)
                return NotFound();
            return Ok(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ProductDto>>Create([FromBody] CreateProductDto dto)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var createdProduct = await _productService.CreateAsync(dto, token);
            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateProductDto dto)
        {
            var result = await _productService.UpdateAsync(id, dto);
            if (!result)
                return NotFound($"Not Found any product match with \"{id}\"");
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteAsync(id);
            if (!result)
                return NotFound($"Not Found any product match with \"{id}\"");
            return NoContent();
        }

        [HttpGet("byCategory/{categoryId}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategoryId(int categoryId)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var allProducts = await _productService.GetAllAsync(token);

            var filtered = allProducts
                .Where(p => p.CategoryId == categoryId)
                .ToList();

            if (filtered.Count == 0)
                return NotFound($"No products found for category ID {categoryId}.");

            return Ok(filtered);
        }
    }
}