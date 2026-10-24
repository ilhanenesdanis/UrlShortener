using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shortener.Shared.Messaging;

internal sealed class RabbitMqConsumer<TMessage>(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ConsumerOptions options,
    ILogger<RabbitMqConsumer<TMessage>> logger) : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        var errorQueue = $"{options.Queue}.error";
        await _channel.QueueDeclareAsync(errorQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Nack edilen mesajlar default exchange ("") üzerinden error kuyruğuna yönlendirilir.
        await _channel.QueueDeclareAsync(
            options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = errorQueue
            },
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(options.Queue, options.Exchange, options.RoutingKey, cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: options.PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(options.Queue, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Consuming {MessageType} from queue {Queue} (exchange: {Exchange}, routing key: {RoutingKey})",
            typeof(TMessage).Name, options.Queue, options.Exchange, options.RoutingKey);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var channel = ((AsyncEventingBasicConsumer)sender).Channel;

        try
        {
            var message = JsonSerializer.Deserialize<TMessage>(args.Body.Span)
                          ?? throw new JsonException($"Message body could not be deserialized to {typeof(TMessage).Name}.");

            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();
            await handler.HandleAsync(message, args.CancellationToken);

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle message {MessageId} from queue {Queue}, moving it to {Queue}.error",
                args.BasicProperties.MessageId, options.Queue, options.Queue);

            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
