using System.Text.Json;
using RabbitMQ.Client;

namespace Shortener.Shared.Messaging;

internal sealed class RabbitMqPublisher(IConnection connection) : IMessagePublisher, IAsyncDisposable
{
    // Aynı channel üzerinden eş zamanlı publish yapılmamalı, bu yüzden tek channel'ı kilitle koruyoruz.
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HashSet<string> _declaredExchanges = [];
    private IChannel? _channel;

    public async Task PublishAsync<TMessage>(string exchange, TMessage message, string routingKey = "", CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString(),
            Type = typeof(TMessage).Name,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            if (!_declaredExchanges.Contains(exchange))
            {
                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
                _declaredExchanges.Add(exchange);
            }

            // Publisher confirms açık olduğu için broker mesajı onaylayana kadar bekler, onaylamazsa exception fırlatır.
            await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, properties, body, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        // Channel seviyesindeki hatalarda channel kapanır ve otomatik kurtarılmaz, yenisini açıyoruz.
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _declaredExchanges.Clear();
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _lock.Dispose();
    }
}
