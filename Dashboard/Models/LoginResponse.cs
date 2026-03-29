using System;
using System.Text.Json.Serialization;

namespace Dashboard.Models
{
    public sealed class LoginResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; init; }

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; init; }
    }
}