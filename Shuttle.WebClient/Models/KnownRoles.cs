namespace Shuttle.WebClient.Models;

public enum KnownRoles {
    Admin,
    Gm
}

public static class RoleNames {
    public const string Admin = "Shuttle.Admin";
}

public static class KnownRolesExtensions {
    public static string ToDisplayString(this KnownRoles role) => role switch {
        KnownRoles.Admin => "Admin",
        KnownRoles.Gm => "GM",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
    
    public static KnownRoles FromString(string value) => value switch {
        "Admin" => KnownRoles.Admin,
        "GM" => KnownRoles.Gm,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
