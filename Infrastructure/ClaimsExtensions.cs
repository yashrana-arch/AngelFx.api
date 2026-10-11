using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace VeltrexAPI.Infrastructure;

public static class ClaimsExtensions
{
    /// <summary>The logged-in user's id from the JWT "sub" claim.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new ApiException(StatusCodes.Status401Unauthorized, "Not authenticated");
}
