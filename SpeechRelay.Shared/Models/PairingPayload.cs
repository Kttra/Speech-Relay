using System.Text.Json.Serialization;

namespace SpeechRelay.Shared.Models;

public class PairingPayload
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int? Port { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
