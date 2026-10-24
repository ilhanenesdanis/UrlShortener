namespace Shortener.Shared.Messaging;

/// <param name="Exchange">Kuyruğun bağlanacağı topic exchange.</param>
/// <param name="Queue">Okunacak kuyruk. Başarısız mesajlar "{Queue}.error" kuyruğuna gider.</param>
/// <param name="RoutingKey">Binding key. "#" exchange'e gelen her mesajı alır.</param>
/// <param name="PrefetchCount">Ack beklenmeden aynı anda alınabilecek mesaj sayısı.</param>
public sealed record ConsumerOptions(string Exchange, string Queue, string RoutingKey = "#", ushort PrefetchCount = 10);
