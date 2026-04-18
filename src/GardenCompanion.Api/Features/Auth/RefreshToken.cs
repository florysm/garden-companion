using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Auth;

// ── Request / Response ──────────────────────────────────────────────────────

public record RefreshTokenCommand(string Token)
    : IRequest<RefreshTokenResponse>;

public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry);

// ── Validator ────────────────────────────────────────────────────────────────

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class RefreshTokenHandler(AppDbContext db, TokenService tokens)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<RefreshTokenResponse> Handle(
        RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.UserRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (existing is null
            || existing.RevokedAt is not null
            || existing.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }

        // Rotate: revoke old, issue new
        existing.RevokedAt = DateTime.UtcNow;

        var now = DateTime.UtcNow;
        var newRefreshTokenValue = tokens.GenerateRefreshToken();
        var newRefreshToken = new UserRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            Token = newRefreshTokenValue,
            ExpiresAt = tokens.RefreshTokenExpiry(),
            CreatedAt = now
        };
        db.UserRefreshTokens.Add(newRefreshToken);

        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokens.GenerateAccessToken(existing.User);

        return new RefreshTokenResponse(
            AccessToken: accessToken,
            RefreshToken: newRefreshTokenValue,
            AccessTokenExpiry: now.AddMinutes(15));
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class RefreshTokenEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", async (
            RefreshTokenCommand command,
            IValidator<RefreshTokenCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("RefreshToken");
    }
}
