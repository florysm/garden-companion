using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Auth;

// ── Request / Response ──────────────────────────────────────────────────────

public record LoginUserCommand(string Email, string Password)
    : IRequest<LoginUserResponse>;

public record LoginUserResponse(
    Guid UserId,
    Guid? HouseholdId,
    string Email,
    string DisplayName,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry);

// ── Validator ────────────────────────────────────────────────────────────────

public class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class LoginUserHandler(AppDbContext db, TokenService tokens)
    : IRequestHandler<LoginUserCommand, LoginUserResponse>
{
    public async Task<LoginUserResponse> Handle(
        LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var householdId = await db.HouseholdMembers
            .Where(m => m.UserId == user.Id)
            .Select(m => (Guid?)m.HouseholdId)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var accessToken = tokens.GenerateAccessToken(user);
        var refreshTokenValue = tokens.GenerateRefreshToken();

        var refreshToken = new UserRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = TokenService.HashToken(refreshTokenValue),
            ExpiresAt = tokens.RefreshTokenExpiry(),
            CreatedAt = now
        };
        db.UserRefreshTokens.Add(refreshToken);

        await db.SaveChangesAsync(cancellationToken);

        return new LoginUserResponse(
            UserId: user.Id,
            HouseholdId: householdId,
            Email: user.Email,
            DisplayName: user.DisplayName,
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            AccessTokenExpiry: tokens.AccessTokenExpiry());
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class LoginUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginUserCommand command,
            IValidator<LoginUserCommand> validator,
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
        .WithName("LoginUser");
    }
}
