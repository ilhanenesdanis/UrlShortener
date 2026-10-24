namespace Shortener.Domain.Entities;

public sealed class ShortUrl : BaseEntity
{
    public string Code { get; private set; } = null!;

    public string OriginalUrl { get; private set; } = null!;

    public long? ExpiresAt { get; private set; }

    public bool IsActive { get; private set; }

    private ShortUrl()
    {
    }

    public static ShortUrl Create(string code, string originalUrl, long? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be empty.", nameof(code));
        }

        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Original URL must be an absolute http or https URL.", nameof(originalUrl));
        }

        if (expiresAt is not null && expiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));
        }

        return new ShortUrl
        {
            Code = code,
            OriginalUrl = originalUrl,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    public bool IsExpired() => ExpiresAt is not null && ExpiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public bool CanRedirect() => IsActive && !IsExpired();

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        MarkAsUpdated();
    }
}
