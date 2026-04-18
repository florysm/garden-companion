using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Users;

// ── Request / Response ───────────────────────────────────────────────────────

public record UpdateMyProfileCommand(Guid UserId, string DisplayName) : IRequest<UserProfileDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record UpdateMyProfileBody(string DisplayName);

public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileBody>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpdateMyProfileHandler(AppDbContext db)
    : IRequestHandler<UpdateMyProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(
        UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        user.DisplayName = request.DisplayName;
        await db.SaveChangesAsync(cancellationToken);

        return new UserProfileDto(user.Id, user.Email, user.DisplayName, user.CreatedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpdateMyProfileEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/me", async (
            UpdateMyProfileBody body,
            IValidator<UpdateMyProfileBody> validator,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var result = await mediator.Send(new UpdateMyProfileCommand(userId, body.DisplayName), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Users")
        .WithName("UpdateMyProfile");
    }
}
