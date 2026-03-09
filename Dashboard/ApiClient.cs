using System.Net.Http;
using System.Net.Http.Headers;

namespace Dashboard;

/// <summary>
/// Provides a single long-lived <see cref="HttpClient"/> instance for all Synk API calls.
/// </summary>
/// <remarks>
/// Repeated <c>new HttpClient() / Dispose()</c> calls exhaust ephemeral ports under load.
/// A shared instance avoids this. Header mutations here are safe because all WPF event
/// handlers execute on the UI thread — there is no concurrent header access in this app.
/// In a multi-threaded or DI-based context, prefer <c>IHttpClientFactory</c> instead.
/// </remarks>
internal static class ApiClient
{
    internal const string BaseUrl = "https://api.synk.hu/";

    private static readonly HttpClient _client = new()
    {
        BaseAddress = new Uri(BaseUrl),
        Timeout = TimeSpan.FromSeconds(30),
    };

    /// <summary>
    /// Returns the shared client with the current session token attached as a Bearer header.
    /// Call this for every authenticated endpoint.
    /// </summary>
    internal static HttpClient WithAuth()
    {
        _client.DefaultRequestHeaders.Authorization =
            AuthSession.Token is { } token
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;

        return _client;
    }

    /// <summary>
    /// Returns the shared client without any Authorization header.
    /// Use this only for public endpoints such as <c>auth/login</c>.
    /// </summary>
    internal static HttpClient Anonymous()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        return _client;
    }
}
