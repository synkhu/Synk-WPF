using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dashboard.Models
{
    public sealed class UsersResponse
    {
        [JsonPropertyName("items")]
        public List<User>? Items { get; init; }

        [JsonPropertyName("page")]
        public int Page { get; init; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; init; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; init; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; init; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; init; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; init; }
    }
}