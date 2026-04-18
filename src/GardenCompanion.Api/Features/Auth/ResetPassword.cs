using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Auth;

// ── Request ──────────────────────────────────────────────────────────────────

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

// ── Validator ────────────────────────────────────────────────────────────────

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class ResetPasswordHandler(AppDbContext db)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var hashedToken = TokenService.HashToken(request.Token);
        var now = DateTime.UtcNow;

        var resetToken = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == hashedToken &&
                t.UsedAt == null &&
                t.ExpiresAt > now,
                cancellationToken);

        if (resetToken is null)
            throw new InvalidOperationException("Token is invalid, expired, or already used.");

        resetToken.UsedAt = now;
        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Revoke all active refresh tokens on password change.
        var activeRefreshTokens = await db.UserRefreshTokens
            .Where(t => t.UserId == resetToken.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeRefreshTokens)
            token.RevokedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class ResetPasswordEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/reset-password", async (
            ResetPasswordCommand command,
            IValidator<ResetPasswordCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                await mediator.Send(command, ct);
                return Results.Ok(new { message = "Password reset successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("ResetPassword");
    }
}
