using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Common;

/// <summary>
/// Shared authorization helpers for the Household domain.
/// </summary>
internal static class HouseholdAccess
{
    /// <summary>
    /// Returns the user's role in the household, or null if they have no membership.
    /// </summary>
    internal static async Task<HouseholdRole?> GetRoleAsync(
        AppDbContext db, Guid householdId, Guid userId, CancellationToken ct)
    {
        return await db.HouseholdMembers
            .Where(m => m.HouseholdId == householdId && m.UserId == userId)
            .Select(m => (HouseholdRole?)m.Role)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Throws KeyNotFoundException when user has no membership.</summary>
    internal static async Task<HouseholdRole> RequireMemberAsync(
        AppDbContext db, Guid householdId, Guid userId, CancellationToken ct)
    {
        var role = await GetRoleAsync(db, householdId, userId, ct);
        if (role is null)
            throw new KeyNotFoundException($"Household {householdId} not found.");
        return role.Value;
    }

    /// <summary>Throws UnauthorizedAccessException when user is not an Owner.</summary>
    internal static async Task RequireOwnerAsync(
        AppDbContext db, Guid householdId, Guid userId, CancellationToken ct)
    {
        var role = await RequireMemberAsync(db, householdId, userId, ct);
        if (role != HouseholdRole.Owner)
            throw new UnauthorizedAccessException("Only the household owner can perform this action.");
    }
}
