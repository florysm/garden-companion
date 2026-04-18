namespace GardenCompanion.Api.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>SHA-256 hash of the raw token. Never store plain text.</summary>
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
