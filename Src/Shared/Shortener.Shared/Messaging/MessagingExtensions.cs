using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shortener.Shared.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Aspire RabbitMQ client'ını (IConnection, health check, tracing) ve IMessagePublisher'ı kaydeder.
    /// connectionName, AppHost'taki RabbitMQ resource adıyla aynı olmalı.
    /// </summary>
    public static IHostApplicationBuilder AddRabbitMqMessaging(this IHostApplicationBuilder builder, string connectionName = "messaging")
    {
        builder.AddRabbitMQClient(connectionName);
        builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        return builder;
    }

    /// <summary>
    /// Kuyruğu dinleyen bir background service ve mesajı işleyecek handler'ı kaydeder.
    /// </summary>
    public static IServiceCollection AddMessageConsumer<TMessage, THandler>(this IServiceCollection services, ConsumerOptions options)
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddScoped<IMessageHandler<TMessage>, THandler>();

        // AddHostedService yerine AddSingleton: aynı mesaj tipi için birden fazla kuyruk dinlenebilsin diye.
        services.AddSingleton<IHostedService>(sp => new RabbitMqConsumer<TMessage>(
            sp.GetRequiredService<IConnection>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            options,
            sp.GetRequiredService<ILogger<RabbitMqConsumer<TMessage>>>()));

        return services;
    }
}
