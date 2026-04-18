using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Common;

/// <summary>
/// Shared authorization helpers for the Garden domain.
/// Access = direct GardenMember OR HouseholdMember of the garden's household.
/// </summary>
internal static class GardenAccess
{
    /// <summary>
    /// Returns the user's effective role in the garden, or null if they have no access.
    /// A direct GardenMember role takes precedence; a HouseholdMember defaults to Contributor.
    /// </summary>
    internal static async Task<GardenRole?> GetRoleAsync(
        AppDbContext db, Guid gardenId, Guid userId, CancellationToken ct)
    {
        var directRole = await db.GardenMembers
            .Where(m => m.GardenId == gardenId && m.UserId == userId)
            .Select(m => (GardenRole?)m.Role)
            .FirstOrDefaultAsync(ct);

        if (directRole is not null)
            return directRole;

        var isHouseholdMember = await db.Gardens
            .Where(g => g.Id == gardenId)
            .AnyAsync(g => g.Household.Members.Any(m => m.UserId == userId), ct);

        return isHouseholdMember ? GardenRole.Contributor : null;
    }

    /// <summary>Throws KeyNotFoundException when user has no access (404 = not found or forbidden).</summary>
    internal static async Task<GardenRole> RequireMemberAsync(
        AppDbContext db, Guid gardenId, Guid userId, CancellationToken ct)
    {
        var role = await GetRoleAsync(db, gardenId, userId, ct);
        if (role is null)
            throw new KeyNotFoundException($"Garden {gardenId} not found.");
        return role.Value;
    }

    /// <summary>Throws UnauthorizedAccessException when user is not an Owner.</summary>
    internal static async Task RequireOwnerAsync(
        AppDbContext db, Guid gardenId, Guid userId, CancellationToken ct)
    {
        var role = await RequireMemberAsync(db, gardenId, userId, ct);
        if (role != GardenRole.Owner)
            throw new UnauthorizedAccessException("Only the garden owner can perform this action.");
    }

    /// <summary>
    /// Resolves garden access for a bed operation.
    /// Looks up the bed's gardenId, then delegates to RequireMemberAsync.
    /// </summary>
    internal static async Task<Guid> RequireBedMemberAsync(
        AppDbContext db, Guid bedId, Guid userId, CancellationToken ct)
    {
        var gardenId = await db.GardenBeds
            .Where(b => b.Id == bedId)
            .Select(b => (Guid?)b.GardenId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Garden bed {bedId} not found.");

        await RequireMemberAsync(db, gardenId, userId, ct);
        return gardenId;
    }

    /// <summary>
    /// Resolves garden access for a planting operation.
    /// Looks up the planting's bed → gardenId, then delegates to RequireMemberAsync.
    /// </summary>
    internal static async Task<Guid> RequirePlantingMemberAsync(
        AppDbContext db, Guid plantingId, Guid userId, CancellationToken ct)
    {
        var gardenId = await db.Plantings
            .Where(p => p.Id == plantingId)
            .Select(p => (Guid?)p.GardenBed.GardenId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Planting {plantingId} not found.");

        await RequireMemberAsync(db, gardenId, userId, ct);
        return gardenId;
    }
}
