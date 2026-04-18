using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Auth;

// ── Request / Response ──────────────────────────────────────────────────────

public record RegisterUserCommand(string Email, string Password, string DisplayName)
    : IRequest<RegisterUserResponse>;

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry);

// ── Validator ────────────────────────────────────────────────────────────────

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.DisplayName)
            .NotEmpty().MaximumLength(100);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class RegisterUserHandler(AppDbContext db, TokenService tokens)
    : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    public async Task<RegisterUserResponse> Handle(
        RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await db.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
            throw new InvalidOperationException("An account with that email already exists.");

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            CreatedAt = now
        };

        var settings = new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };

        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = $"{request.DisplayName}'s Garden",
            OwnedByUserId = user.Id,
            CreatedAt = now
        };

        var membership = new HouseholdMember
        {
            Id = Guid.NewGuid(),
            HouseholdId = household.Id,
            UserId = user.Id,
            Role = HouseholdRole.Owner,
            JoinedAt = now
        };

        db.Users.Add(user);
        db.UserSettings.Add(settings);
        db.Households.Add(household);
        db.HouseholdMembers.Add(membership);

        var accessToken = tokens.GenerateAccessToken(user);
        var refreshTokenValue = tokens.GenerateRefreshToken();

        var refreshToken = new UserRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = tokens.RefreshTokenExpiry(),
            CreatedAt = now
        };
        db.UserRefreshTokens.Add(refreshToken);

        await db.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            AccessTokenExpiry: tokens.AccessTokenExpiry());
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class RegisterUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            RegisterUserCommand command,
            IValidator<RegisterUserCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/users/{result.UserId}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("RegisterUser");
    }
}
