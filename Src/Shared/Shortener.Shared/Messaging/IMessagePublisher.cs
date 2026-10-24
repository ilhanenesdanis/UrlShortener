namespace Shortener.Shared.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(string exchange, TMessage message, string routingKey = "", CancellationToken cancellationToken = default);
}
