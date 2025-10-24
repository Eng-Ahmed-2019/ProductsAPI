using Microsoft.Extensions.Configuration;
using ProductBusiness.Interfaces;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace ProductBusiness.Services
{
    public class RabbitMqMessageBus : IMessageBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly RabbitMQ.Client.IModel _channel;

        public RabbitMqMessageBus(IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: configuration["RabbitMQ:ExchangeName"],
                type: ExchangeType.Direct,
                durable: true
            );

            _channel.QueueDeclare(
                queue: configuration["RabbitMQ:QueueName"],
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _channel.QueueBind(
                queue: configuration["RabbitMQ:QueueName"],
                exchange: configuration["RabbitMQ:ExchangeName"],
                routingKey: configuration["RabbitMQ:RoutingKey"]
            );
        }

        public void Publish<T>(T message, string exchange, string routingKey)
        {
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: null,
                body: body
            );
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}