using System.Security.Claims;

namespace tunav_backend.Authorization;

/// <summary>
/// Authorization rules: legacy roles (Admin, Marketing, …) OR fine-grained permission claims from <see cref="TunavClaimTypes.Permission"/>.
/// </summary>
public static class TunavAuthorization
{
    public static bool HasPermission(ClaimsPrincipal user, string code) =>
        user.HasClaim(TunavClaimTypes.Permission, code);

    public static bool HasAnyPermissionPrefix(ClaimsPrincipal user, string prefix) =>
        user.FindAll(TunavClaimTypes.Permission)
            .Any(c => c.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static bool IsInAnyRole(ClaimsPrincipal user, params string[] roles) =>
        roles.Any(user.IsInRole);

    public static bool CanManageUsers(ClaimsPrincipal u) =>
        u.IsInRole("Admin") || HasPermission(u, "system.users.manage");

    public static bool CanManageRoles(ClaimsPrincipal u) =>
        u.IsInRole("Admin") || HasPermission(u, "system.roles.manage");

    public static bool CanManagePermissionsMatrix(ClaimsPrincipal u) =>
        u.IsInRole("Admin") || HasPermission(u, "system.permissions.manage");

    public static bool CanBlogWrite(ClaimsPrincipal u) =>
        IsInAnyRole(u, "Admin", "Marketing") || HasAnyPermissionPrefix(u, "blog.");

    /// <summary>Events CRUD + training content (not collaboration-only).</summary>
    public static bool CanEventWrite(ClaimsPrincipal u)
    {
        if (IsInAnyRole(u, "Admin", "Marketing")) return true;
        if (HasAnyPermissionPrefix(u, "training.")) return true;

        foreach (var c in u.FindAll(TunavClaimTypes.Permission))
        {
            var v = c.Value;
            if (v.StartsWith("event.", StringComparison.OrdinalIgnoreCase)
                && !v.StartsWith("event.collaboration", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool CanCollaborationRead(ClaimsPrincipal u) =>
        IsInAnyRole(u, "Admin", "Marketing", "Ressources Humaines")
        || HasAnyPermissionPrefix(u, "event.collaboration.");

    public static bool CanHrWrite(ClaimsPrincipal u) =>
        IsInAnyRole(u, "Admin", "Ressources Humaines") || HasAnyPermissionPrefix(u, "job.");

    public static bool CanNewsletterWrite(ClaimsPrincipal u) =>
        IsInAnyRole(u, "Admin", "Marketing") || HasAnyPermissionPrefix(u, "newsletter.");

    public static bool CanContactRead(ClaimsPrincipal u) =>
        IsInAnyRole(u, "Admin", "Ressources Humaines", "Commercial")
        || HasAnyPermissionPrefix(u, "contact.");

    public static bool CanSolutionWrite(ClaimsPrincipal u) =>
        IsInAnyRole(u, "Admin", "Marketing") || HasPermission(u, "solution.manage");

    public static bool CanTeamWrite(ClaimsPrincipal u) =>
        IsInAnyRole(u, "Admin", "Marketing") || HasPermission(u, "team.manage");
}
