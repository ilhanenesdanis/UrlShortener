namespace Shortener.Domain.Entities;

/// <summary>
/// Tüm entity'lerin ortak alanları. Tarihler UTC Unix timestamp (saniye) olarak tutulur, örn. 1792876509.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public long CreatedAt { get; protected set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public long? UpdatedAt { get; protected set; }

    public void MarkAsUpdated() => UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
