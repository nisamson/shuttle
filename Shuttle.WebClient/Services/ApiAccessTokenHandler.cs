using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Shuttle.WebClient.Services;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches the signed-in user's access token to outgoing
/// requests to the Shuttle backend API. Because the API lives on a different origin than the
/// WebClient, the built-in <c>BaseAddressAuthorizationMessageHandler</c> won't attach tokens, so we
/// request one explicitly here.
/// <para>
/// The backend endpoints this handler is used for are <c>[AllowAnonymous]</c> but return richer data
/// (e.g. Discord names) to authenticated callers. So when no token is available — the user is
/// anonymous, or acquisition fails — the request simply proceeds without an <c>Authorization</c>
/// header rather than failing, keeping public browsing working.
/// </para>
/// </summary>
public sealed class ApiAccessTokenHandler : DelegatingHandler {
    private readonly IAccessTokenProvider tokenProvider;

    public ApiAccessTokenHandler(IAccessTokenProvider tokenProvider) {
        this.tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        try {
            var result = await tokenProvider.RequestAccessToken();
            if (result.TryGetToken(out var token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
            }
        } catch (AccessTokenNotAvailableException) {
            // Anonymous caller (or interactive consent required) — proceed without a token so the
            // AllowAnonymous endpoints still serve their public projection.
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
