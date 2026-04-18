using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Infrastructure.Data;
using GardenCompanion.Api.Infrastructure.Email;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Auth;

// ── Request ──────────────────────────────────────────────────────────────────

public record ForgotPasswordCommand(string Email) : IRequest;

// ── Validator ────────────────────────────────────────────────────────────────

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class ForgotPasswordHandler(
    AppDbContext db,
    IEmailService email,
    IConfiguration configuration,
    ILogger<ForgotPasswordHandler> logger)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Always return success to prevent email enumeration.
        if (user is null)
        {
            logger.LogInformation("Password reset requested for unknown email {Email}", request.Email);
            return;
        }

        var plainToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                         Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var hashedToken = TokenService.HashToken(plainToken);
        var now = DateTime.UtcNow;

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = hashedToken,
            ExpiresAt = now.AddHours(1)
        };
        db.PasswordResetTokens.Add(resetToken);
        await db.SaveChangesAsync(cancellationToken);

        var frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(plainToken)}";

        try
        {
            await email.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email for user {UserId}", user.Id);
            // Do not surface the error — the token is persisted; user can retry.
        }
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class ForgotPasswordEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/forgot-password", async (
            ForgotPasswordCommand command,
            IValidator<ForgotPasswordCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            await mediator.Send(command, ct);
            // Always 200 — prevents email enumeration.
            return Results.Ok(new { message = "If an account exists for that email, a reset link has been sent." });
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("ForgotPassword");
    }
}
