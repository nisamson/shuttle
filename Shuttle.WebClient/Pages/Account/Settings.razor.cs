using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Users;

namespace Shuttle.WebClient.Pages.Account;

public partial class Settings : ComponentBase {
    [Inject]
    public required IShuttleUserClient UserClient { private get; set; }

    private readonly SettingsModel model = new();
    private CurrentUser? current;
    private bool loading = true;
    private bool saving;
    private string? loadError;
    private string? saveError;
    private bool saved;

    private string AccountId => current?.Id.ToString() ?? string.Empty;

    protected override async Task OnInitializedAsync() {
        try {
            current = await UserClient.GetCurrentUser();
            model.Username = current.Username;
        } catch (Exception ex) {
            loadError = ex.Message;
        } finally {
            loading = false;
        }
    }

    private async Task SaveAsync() {
        saving = true;
        saveError = null;
        saved = false;
        try {
            current = await UserClient.UpdateCurrentUser(new UpdateCurrentUserRequest { Username = model.Username });
            model.Username = current.Username;
            saved = true;
        } catch (ValidationApiException) {
            saveError = "That username is invalid. Use 2-32 letters, digits, periods, or underscores.";
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict) {
            saveError = "That username is already taken. Please choose another one.";
        } catch (Exception ex) {
            saveError = ex.Message;
        } finally {
            saving = false;
        }
    }

    public sealed class SettingsModel {
        [Required(ErrorMessage = "A username is required.")]
        [RegularExpression(
            "^[A-Za-z0-9._]{2,32}$",
            ErrorMessage = "Username must be 2-32 characters: letters, digits, periods, or underscores.")]
        public string Username { get; set; } = string.Empty;
    }
}
