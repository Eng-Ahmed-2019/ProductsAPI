using System.Text;
using RabbitMQ.Client;
using System.Text.Json;
using ProductData.Interfaces;
using RabbitMQ.Client.Events;
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
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            var queueName = _configuration["RabbitMQ:QueueName"] ?? "product_updates";

            await channel.QueueDeclareAsync(queue: queueName,
                                            durable: false,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null
            );

            Console.WriteLine($" [Products] Waiting for messages in \"{queueName}\"...");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [Products] Received: {json}");

                try
                {
                    var message = JsonSerializer.Deserialize<OrderProductMessage>(json);
                    if (message != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                        var product = await repo.GetByIdAsync(message.ProductId);
                        if (product != null)
                        {
                            product.Stock -= message.QuantityOrdered;
                            if (product.Stock < 0) product.Stock = 0;

                            repo.Update(product);
                            await repo.SaveChangesAsync();

                            Console.WriteLine($" [Products] Updated Product {product.Id} | Remaining Stock: {product.Stock}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" [Products] Error processing message: {ex.Message}");
                }
                await Task.Yield();
            };

            await channel.BasicConsumeAsync(queue: queueName,
                                            autoAck: true,
                                            consumer: consumer
            );
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private class OrderProductMessage
        {
            public int OrderId { get; set; }
            public int ProductId { get; set; }
            public int QuantityOrdered { get; set; }
        }
    }
}