using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Shuttle.WebClient.Components.Dev;

public partial class HelloApiView : ComponentBase {
    private bool loading;
    private string? message;
    private string? error;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync() {
        loading = true;
        error = null;
        try {
            var response = await Http.GetFromJsonAsync<HelloResponse>("hello");
            message = response?.Message ?? "(no message)";
        } catch (Exception ex) {
            error = ex.Message;
        } finally {
            loading = false;
        }
    }

    private sealed record HelloResponse(string Message);
}
