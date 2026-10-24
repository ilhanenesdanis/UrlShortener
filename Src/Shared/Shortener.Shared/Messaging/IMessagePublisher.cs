namespace Shortener.Shared.Messaging;

public interface IMessagePublisher
{
    /// <summary>
    /// Mesajı JSON olarak verilen topic exchange'e yayınlar. Exchange yoksa oluşturulur.
    /// </summary>
    Task PublishAsync<TMessage>(string exchange, TMessage message, string routingKey = "", CancellationToken cancellationToken = default);
}
