using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shortener.Shared.Messaging;

public static class MessagingExtensions
{
    public static IHostApplicationBuilder AddRabbitMqMessaging(this IHostApplicationBuilder builder, string connectionName = "messaging")
    {
        builder.AddRabbitMQClient(connectionName);
        builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        return builder;
    }

    public static IServiceCollection AddMessageConsumer<TMessage, THandler>(this IServiceCollection services, ConsumerOptions options)
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddScoped<IMessageHandler<TMessage>, THandler>();

        services.AddSingleton<IHostedService>(sp => new RabbitMqConsumer<TMessage>(
            sp.GetRequiredService<IConnection>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            options,
            sp.GetRequiredService<ILogger<RabbitMqConsumer<TMessage>>>()));

        return services;
    }
}
