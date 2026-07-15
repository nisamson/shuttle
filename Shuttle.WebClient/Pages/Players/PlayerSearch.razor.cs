using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shuttle.WebClient.Models;

namespace Shuttle.WebClient.Pages.Players;

public partial class PlayerSearch : ComponentBase {
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private string? playerIdText;

    private bool CanGo => TryParsePlayerId(out _);

    private bool TryParsePlayerId(out int playerId) =>
        int.TryParse(playerIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out playerId) && playerId > 0;

    private void Go() {
        if (TryParsePlayerId(out var id)) {
            Navigation.NavigateTo(Routes.Players.Player(id));
        }
    }

    private void OnKeyDown(KeyboardEventArgs args) {
        if (args.Key is "Enter" or "NumpadEnter") {
            Go();
        }
    }
}
