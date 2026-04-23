# Garden Companion — New Backend Vertical Slice

Create a new vertical slice for the Garden Companion API following the established pattern exactly.

## Slice anatomy

Each slice lives in a **single `.cs` file** under `src/GardenCompanion.Api/Features/{Feature}/`:

```
// ── Request / Response ──────────────────────────────────────────────────────
public record MyCommand(...) : IRequest<MyDto>;          // or IRequest (void)

// ── Validator ────────────────────────────────────────────────────────────────
public class MyCommandValidator : AbstractValidator<MyCommand> { ... }

// ── Handler ──────────────────────────────────────────────────────────────────
public class MyCommandHandler(AppDbContext db) : IRequestHandler<MyCommand, MyDto>
{
    public async Task<MyDto> Handle(MyCommand request, CancellationToken ct) { ... }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────
public static class MyCommandEndpoint
{
    public record Request(...);   // HTTP body shape (strings for enums)

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/route", async (...) => { ... })
           .RequireAuthorization()
           .WithTags("FeatureName")
           .WithName("OperationName");
    }
}
```

## Authorization helpers

```csharp
// Any garden member (or household member of garden's household):
await GardenAccess.RequireMemberAsync(db, gardenId, userId, ct);

// Garden owner only:
await GardenAccess.RequireOwnerAsync(db, gardenId, userId, ct);

// Garden bed membership:
await GardenAccess.RequireBedMemberAsync(db, bedId, userId, ct);

// Get role without throwing:
var role = await GardenAccess.GetRoleAsync(db, gardenId, userId, ct);
```

## Error → HTTP mapping (wired in `Program.cs`)

| Exception | HTTP |
|---|---|
| `KeyNotFoundException` | 404 (also used for "no access" to prevent info leakage) |
| `UnauthorizedAccessException` | 403 |
| `InvalidOperationException` | 409 |
| FluentValidation failure | 422 via `Results.ValidationProblem` |

## Getting the current user

```csharp
var userId = ctx.User.GetUserId();   // reads "sub" claim — MapInboundClaims = false
```

## Enum handling in endpoints

Parse string → enum in the endpoint, not the handler:
```csharp
if (!Enum.TryParse<TaskType>(req.TaskType, true, out var taskType))
    return Results.ValidationProblem(new Dictionary<string, string[]>
        { ["taskType"] = [$"Invalid task type '{req.TaskType}'."] });
```

## Register the endpoint

After creating the file, add `.Map(app)` call in `Program.cs` alongside the other feature endpoints in the same feature group.

## DTOs

If the feature already has a `{Feature}Dtos.cs` file, add the new DTO record there. If it's the first slice in the feature, create `{Feature}Dtos.cs`.

## Checklist

- [ ] Single file per operation under `Features/{Feature}/`
- [ ] Command/Query record + Validator + Handler + Endpoint all in the file
- [ ] Authorization checked at top of handler using GardenAccess helpers
- [ ] Foreign keys validated before any writes
- [ ] Endpoint registered in `Program.cs`
- [ ] DTO added to `{Feature}Dtos.cs`
- [ ] Soft-delete filter respected (Plantings have `DeletedAt` global filter — no manual where clause needed)
