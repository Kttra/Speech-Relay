using System.Text.Json.Serialization;

namespace SpeechRelay.Shared.Models;

public class SpeechRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = string.Empty;

    [JsonPropertyName("generator")]
    public AudioGenerator Generator { get; set; } = AudioGenerator.WindowsNarrator;
}
