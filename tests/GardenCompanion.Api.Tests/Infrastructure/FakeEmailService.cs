namespace GardenCompanion.Api.Tests.Infrastructure;

public sealed class FakeEmailService : IEmailService
{
    public List<SentPasswordResetEmail> SentEmails { get; } = [];

    public Task SendPasswordResetEmailAsync(
        string to,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        SentEmails.Add(new SentPasswordResetEmail(to, resetLink));
        return Task.CompletedTask;
    }
}

public sealed record SentPasswordResetEmail(string To, string ResetLink);
