using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GardenCompanion.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(value);
    }
}
