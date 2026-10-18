using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeechRelay.Shared.Models;

public static class SpeechRelayJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}