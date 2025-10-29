using System.Text;
using RabbitMQ.Client;
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
                Port = 5672, // ✅ أضف المنفذ بوضوح
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
            };

            var retries = 5;
            while (retries-- > 0 && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var connection = await factory.CreateConnectionAsync();
                    using var channel = await connection.CreateChannelAsync();

                    var queueName = _configuration["RabbitMQ:QueueName"] ?? "product_updates";

                    await channel.QueueDeclareAsync(
                        queue: queueName,
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
                        await Task.Yield();
                    };

                    await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

                    await Task.Delay(Timeout.Infinite, stoppingToken);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" [RabbitMQ] Connection failed: {ex.Message}. Retrying...");
                    await Task.Delay(3000, stoppingToken);
                }
            }
        }

        private class OrderProductMessage
        {
            public int OrderId { get; set; }
            public int ProductId { get; set; }
            public int QuantityOrdered { get; set; }
        }
    }
}