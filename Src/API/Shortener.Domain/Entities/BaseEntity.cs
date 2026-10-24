namespace Shortener.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public long CreatedAt { get; protected set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public long? UpdatedAt { get; protected set; }

    public void MarkAsUpdated() => UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
