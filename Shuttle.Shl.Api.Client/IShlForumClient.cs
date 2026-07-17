namespace Shuttle.Shl.Api.Client;

/// <summary>
/// Client for scraping data from the SHL community forum (<c>simulationhockey.com</c>) that is not
/// exposed through the Index or Portal APIs — currently, a member's self-reported Discord username.
/// </summary>
public interface IShlForumClient {
    /// <summary>
    /// Fetches the member's forum profile page and extracts the Discord username they have listed on
    /// it, if any.
    /// </summary>
    /// <param name="userId">The forum member id (as used in <c>member.php?action=profile&amp;user=</c>).</param>
    /// <param name="token">A token to cancel the outbound request and page read.</param>
    /// <returns>
    /// The member's Discord username, or <see langword="null"/> when the profile lists none (or no
    /// Discord field is present on the page).
    /// </returns>
    public Task<string?> GetDiscordUsername(int userId, CancellationToken token = default);
}
