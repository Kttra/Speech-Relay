using System;
using System.Threading.Tasks;

namespace PcSpeakerApp;

public interface IWebServer
{
    event Action<string> TextReceived;
    Task<byte[]> PiperSpeakAsync(string text);
    void PlayAudio(byte[] audio);
    void Start();
}
