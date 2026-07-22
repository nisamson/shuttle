// Workaround for https://github.com/dotnet/aspnetcore/issues/64009
//
// In .NET 10, Blazor WebAssembly stopped caching boot resources in the browser's
// Cache Storage and relies solely on the HTTP cache. Chromium browsers (Chrome/Edge)
// do NOT honor the `immutable` Cache-Control directive
// (https://issues.chromium.org/issues/41253661), so on every load/reload they
// revalidate each fingerprinted boot resource, producing a slow sequential waterfall
// of `If-None-Match` -> `304 Not Modified` requests that delays startup.
//
// Every file under `_framework/` is content-fingerprinted (the content hash is part of
// the filename), so it is safe to serve straight from cache without revalidation:
// if the content ever changes, the filename changes too. Forcing `cache: 'force-cache'`
// for these requests makes the browser use the cached copy directly (shown as
// "(disk cache)" in DevTools) and eliminates the revalidation round-trips.
(function () {
    const originalFetch = window.fetch;
    const bootResource = /\/_framework\/[^?]*\.(wasm|dat|js)(\?|$)/i;

    window.fetch = function (resource, options) {
        options = options || {};
        const url = typeof resource === "string" ? resource : (resource && resource.url);
        if (url && bootResource.test(url)) {
            options = Object.assign({}, options, { cache: "force-cache" });
        }
        return originalFetch.call(this, resource, options);
    };
})();
