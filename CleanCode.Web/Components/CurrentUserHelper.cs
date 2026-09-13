using System.Security.Claims;

namespace CleanCode.Web.Components;

/// <summary>
/// Helper for extracting the current user's identifier from their claims.
/// This matches the user ID value stored on albums and photos.
/// </summary>
public static class CurrentUserHelper
{
    /// <summary>
    /// Returns the user ID for the given principal, using the "email" claim
    /// or falling back to the identity name. Returns null when the user is
    /// not authenticated or no identifier can be resolved.
    /// </summary>
    public static string? GetUserId(ClaimsPrincipal? user)
    {
        if (user is null)
            return null;

        var emailClaim = user.Claims.FirstOrDefault(c => c.Type == "email");
        return emailClaim?.Value ?? user.Identity?.Name;
    }
}
