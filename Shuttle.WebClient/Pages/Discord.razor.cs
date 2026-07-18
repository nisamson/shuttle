using Microsoft.AspNetCore.Components;
using Shuttle.Api.Client;
using Shuttle.Models.Users;

namespace Shuttle.WebClient.Pages;

/// <summary>
/// Development-only diagnostic page that looks up a forum member's Discord username through the
/// backend debug endpoint (<c>GET /debug/users/{userId}/discord</c>), which scrapes the member's
/// forum profile on the server.
/// </summary>
public partial class Discord : ComponentBase {
    [Inject] private IShuttleDebugClient DebugClient { get; set; } = null!;

    private string? UserIdText { get; set; }
    private bool Loading { get; set; }
    private DiscordUsernameResult? Result { get; set; }
    private string? Error { get; set; }

    private int? ParsedUserId =>
        int.TryParse(UserIdText, out var id) && id > 0 ? id : null;

    private async Task OnKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e) {
        if (e.Key == "Enter") {
            await LookupAsync();
        }
    }

    private async Task LookupAsync() {
        if (ParsedUserId is not { } userId) {
            return;
        }

        Loading = true;
        Error = null;
        Result = null;
        try {
            Result = await DebugClient.GetDiscordUsername(userId);
        } catch (Exception ex) {
            Error = $"The API did not return a result ({ex.Message}). " +
                "The debug endpoint is only available when the API itself runs in Development.";
        } finally {
            Loading = false;
        }
    }
}
