using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ProductBusiness.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProductBusiness.Services
{
    public class ConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public ConsumerService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                Port = 5672
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            var queueName = _configuration["RabbitMQ:QueueName"] ?? "product_updates";

            await channel.QueueDeclareAsync(queue: queueName,
                                            durable: false,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null);

            Console.WriteLine($" [Products] Waiting for messages in \"{queueName}\"...");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);

                    Console.WriteLine($" [Products] Received: {json}");

                    var message = JsonSerializer.Deserialize<OrderProductMessage>(json);

                    if (message != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                        await UpdateProductStockAsync(productService, message.ProductId, message.QuantityOrdered);
                    }

                    await Task.Yield();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" [Products] Error processing message: {ex.Message}");
                }
            };

            await channel.BasicConsumeAsync(queue: queueName,
                                            autoAck: true,
                                            consumer: consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task UpdateProductStockAsync(IProductService productService, int productId, int quantityOrdered)
        {
            var product = await productService.GetByIdAsync(productId, "");
            if (product == null)
            {
                Console.WriteLine($" [Products] Product {productId} not found.");
                return;
            }

            if (product.Stock < quantityOrdered)
            {
                Console.WriteLine($" [Products] Not enough stock for product {productId}.");
                return;
            }

            var newStock = product.Stock - quantityOrdered;

            var updateDto = new ProductDTOs.CreateProductDto
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = newStock,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId
            };

            var result = await productService.UpdateAsync(productId, updateDto);
            if (result)
                Console.WriteLine($" [Products] Stock updated for product {productId}. New stock: {newStock}");
            else
                Console.WriteLine($" [Products] Failed to update stock for product {productId}.");
        }

        private class OrderProductMessage
        {
            public int OrderId { get; set; }
            public int ProductId { get; set; }
            public int QuantityOrdered { get; set; }
        }
    }
}