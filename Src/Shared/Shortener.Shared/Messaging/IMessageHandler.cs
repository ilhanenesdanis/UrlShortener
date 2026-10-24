namespace Shortener.Shared.Messaging;

/// <summary>
/// Kuyruktan gelen mesajı işler. Her mesaj için yeni bir scope açılır, scoped servisler (DbContext vb.) inject edilebilir.
/// Exception fırlatırsa mesaj "{queue}.error" kuyruğuna düşer.
/// </summary>
public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
