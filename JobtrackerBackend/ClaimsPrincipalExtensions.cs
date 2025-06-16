using System.Security.Claims;

namespace JobtrackerBackend;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId( this ClaimsPrincipal claimsPrincipal)
    {
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ??
                          claimsPrincipal.FindFirst("sub");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
        {
            return userId;
        }

        return null;
    }
}