using System.Security.Claims;

namespace JobtrackerBackend;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
    } 
    public static string? GetToken(this ClaimsPrincipal user)
    {
        var identity = user.Identity as ClaimsIdentity;
        return identity?.BootstrapContext as string;
    }
}