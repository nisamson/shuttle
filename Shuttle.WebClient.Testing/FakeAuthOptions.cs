namespace Shuttle.WebClient.Testing;

/// <summary>
/// Configures the identity presented by <see cref="FakeAuthenticationStateProvider"/> when the
/// WebClient runs in fake-backend mode (or in a bUnit test). Lets tests flip between anonymous and
/// authenticated users and grant arbitrary roles without any Entra / MSAL round trip.
/// </summary>
public sealed class FakeAuthOptions {
    /// <summary>Whether the fake user is signed in. When <see langword="false"/>, an anonymous principal is used.</summary>
    public bool IsAuthenticated { get; set; } = true;

    /// <summary>The display name / <c>name</c> claim of the fake user.</summary>
    public string UserName { get; set; } = "Test Scout";

    /// <summary>The stable object id (<c>oid</c>) claim of the fake user.</summary>
    public string UserId { get; set; } = "00000000-0000-0000-0000-000000000001";

    /// <summary>
    /// Roles granted to the fake user. Defaults to the WebClient admin role so every page — including
    /// role-gated admin pages — is reachable when testing. Clear it to exercise the unauthorized path.
    /// </summary>
    public IList<string> Roles { get; set; } = new List<string> { "Shuttle.Admin" };
}
