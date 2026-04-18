using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Features.Gardens;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenMembers;

// ── Request / Response ──────────────────────────────────────────────────────

public record AddGardenMemberCommand(
    Guid CurrentUserId,
    Guid GardenId,
    string Email,
    GardenRole Role) : IRequest<GardenMemberDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public class AddGardenMemberValidator : AbstractValidator<AddGardenMemberCommand>
{
    public AddGardenMemberValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class AddGardenMemberHandler(AppDbContext db)
    : IRequestHandler<AddGardenMemberCommand, GardenMemberDto>
{
    public async Task<GardenMemberDto> Handle(
        AddGardenMemberCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireOwnerAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var targetUser = await db.Users
            .FirstOrDefaultAsync(
                u => u.Email == request.Email.ToLowerInvariant(), cancellationToken)
            ?? throw new KeyNotFoundException($"No user found with email '{request.Email}'.");

        var alreadyMember = await db.GardenMembers
            .AnyAsync(
                m => m.GardenId == request.GardenId && m.UserId == targetUser.Id,
                cancellationToken);

        if (alreadyMember)
            throw new InvalidOperationException(
                $"'{request.Email}' is already a member of this garden.");

        var member = new GardenMember
        {
            Id = Guid.NewGuid(),
            GardenId = request.GardenId,
            UserId = targetUser.Id,
            Role = request.Role,
            InvitedByUserId = request.CurrentUserId
        };

        db.GardenMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return new GardenMemberDto(targetUser.Id, targetUser.DisplayName, request.Role.ToString());
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class AddGardenMemberEndpoint
{
    public record Request(string Email, string Role);

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/gardens/{gardenId:guid}/members", async (
            Guid gardenId,
            Request req,
            HttpContext ctx,
            IValidator<AddGardenMemberCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();

            if (!Enum.TryParse<GardenRole>(req.Role, true, out var role))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["role"] = [$"Invalid role '{req.Role}'. Use Owner or Contributor."] });

            var command = new AddGardenMemberCommand(userId, gardenId, req.Email, role);

            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/gardens/{gardenId}/members", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("GardenMembers")
        .WithName("AddGardenMember");
    }
}
