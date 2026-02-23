using System.Security.Claims;

namespace PennaiWise.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the authenticated user's numeric ID from the "sub" JWT claim.
    /// Returns null if the claim is missing or cannot be parsed.
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        // ASP.NET Core's JWT middleware maps "sub" → ClaimTypes.NameIdentifier
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? user.FindFirstValue("sub");

        return int.TryParse(value, out var id) ? id : null;
    }
}
