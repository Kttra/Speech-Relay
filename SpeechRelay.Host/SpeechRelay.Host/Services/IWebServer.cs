using System;
using System.Threading.Tasks;
using static SpeechRelay.Host.Services.WebServer;

namespace SpeechRelay.Host.Services;

public interface IWebServer
{
    event Action<string> TextReceived;
    Task<bool> ProcessVoice(string text, string voice, AudioGenerator audioGenerator);
    void Start();
}
