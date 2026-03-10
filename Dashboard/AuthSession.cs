namespace Dashboard;

/// <summary>
/// Holds authentication state for the active user session.
/// Replaces untyped <see cref="System.Windows.Application.Properties"/> access with a
/// compile-time-safe surface area.
/// </summary>
internal static class AuthSession
{
    internal static string? Token { get; set; }
    internal static DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Returns <c>true</c> when a token is present but its server-reported expiry has passed.
    /// This is a client-side early-out; the server always has final authority.
    /// </summary>
    internal static bool IsExpired =>
        ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;

    internal static void Clear()
    {
        Token = null;
        ExpiresAt = null;
    }
}
