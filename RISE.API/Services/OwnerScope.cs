using System.Security.Claims;

namespace RISE.API.Services;

/// <summary>
/// Resolves the authenticated account id used to scope all operational records.
/// </summary>
public static class OwnerScope
{
    public static int UserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("userId")?.Value;
        return int.TryParse(raw, out var id) ? id : 0;
    }
}
