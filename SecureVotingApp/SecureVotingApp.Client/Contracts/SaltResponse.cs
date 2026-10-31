using System.Text.Json.Serialization;

namespace SecureVotingApp.Components.Contracts;

public sealed class SaltResponse
{
    [JsonPropertyName("details")]
    public string Salt { get; set; } = string.Empty;
}