namespace GardenCompanion.Api.Infrastructure.Email;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default);
}
