using System;
using System.Threading.Tasks;
using static PcSpeakerApp.Services.WebServer;

namespace PcSpeakerApp.Services;

public interface IWebServer
{
    event Action<string> TextReceived;
    Task<bool> ProcessVoice(string text, string voice, AudioGenerator audioGenerator);
    void Start();
}
