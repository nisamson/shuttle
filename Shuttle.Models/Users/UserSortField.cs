namespace Shuttle.Models.Users;

/// <summary>The field a user search is sorted by.</summary>
public enum UserSortField {
    /// <summary>Sort by username (the default).</summary>
    Username,

    /// <summary>Sort by numeric user id.</summary>
    UserId,

    /// <summary>
    /// Sort by Discord name. Only meaningful for authenticated callers (Discord names are hidden
    /// otherwise); anonymous callers effectively sort by a blank key.
    /// </summary>
    DiscordName,
}
