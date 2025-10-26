using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;

namespace ProductBusiness.Services
{
    public class ProducerService
    {
        private readonly IConfiguration _configuration;

        public ProducerService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendMessageAsync(string message)
        {
            var hostName = _configuration["RabbitMQ:HostName"];
            var userName = _configuration["RabbitMQ:UserName"];
            var password = _configuration["RabbitMQ:Password"];
            var queueName = _configuration["RabbitMQ:QueueName"];

            var factory = new ConnectionFactory
            {
                HostName = hostName ?? "localhost",
                UserName = userName ?? "guest",
                Password = password ?? "guest"
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: queueName ?? "hello",
                                            durable: false,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null
            );

            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(exchange: string.Empty,
                                            routingKey: queueName ?? "hello",
                                            body: body
            );

            Console.WriteLine($" [Products] Sent {message}");
        }
    }
}