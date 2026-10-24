namespace Shortener.Shared.Messaging;

public sealed record ConsumerOptions(string Exchange, string Queue, string RoutingKey = "#", ushort PrefetchCount = 10);
