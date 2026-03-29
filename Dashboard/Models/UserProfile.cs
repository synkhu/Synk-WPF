using System;
using System.Text.Json.Serialization;

namespace Dashboard.Models
{
    public sealed class UserProfile
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; init; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; init; }

        [JsonPropertyName("profilePictureUrl")]
        public string? ProfilePictureUrl { get; init; }

        [JsonPropertyName("role")]
        public string? Role { get; init; }

        [JsonPropertyName("emailVerified")]
        public bool EmailVerified { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; init; }
    }
}