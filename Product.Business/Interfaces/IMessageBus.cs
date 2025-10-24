namespace ProductBusiness.Interfaces
{
    public interface IMessageBus
    {
        void Publish<T>(T message, string exchange, string routingKey);
    }
}