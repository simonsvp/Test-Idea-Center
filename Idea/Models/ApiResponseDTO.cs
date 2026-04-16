using System.Text.Json.Serialization;

namespace Idea.Models
{
    internal class ApiResponseDTO
    {
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("ideaId")]
        public string? IdeaId { get; set; }
    }
}