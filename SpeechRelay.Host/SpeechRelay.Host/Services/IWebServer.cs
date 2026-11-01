using SpeechRelay.Shared.Models;
using System;
using System.Threading.Tasks;

namespace SpeechRelay.Host.Services;

public interface IWebServer
{
    event Action<string, string, AudioGenerator> TextReceived;
    Task<bool> ProcessVoice(string text, string voice, AudioGenerator audioGenerator);
    void Start();
}
