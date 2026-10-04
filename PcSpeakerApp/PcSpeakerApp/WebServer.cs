using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace PcSpeakerApp;

public class WebServer : IWebServer
{
    //private readonly MainWindowViewModel _vm;
    public event Action<string>? TextReceived;
    private readonly IConfiguration _config;

    public WebServer(IConfiguration config)
    {
        //_vm = vm;
        _config = config;
    }

    public void Start()
    {
        Task.Run(() =>
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://*:5000/speak/");
            listener.Start();

            while (true)
            {
                var ctx = listener.GetContext();
                HandleRequest(ctx);
            }
        });
    }

    private async void HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            var providedToken = ctx.Request.Headers["X-Auth-Token"];
            if (providedToken != _config["Auth:Token"])
            {
                ctx.Response.StatusCode = 401;
                Write(ctx, "Unauthorized");
                return;
            }

            string text = "";
            string voice = "en-US-OnyxTurboMultilingualNeural";

            if (ctx.Request.HttpMethod == "POST")
            {
                using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
                var body = await reader.ReadToEndAsync();

                // Parse form data
                var parsed = System.Web.HttpUtility.ParseQueryString(body);
                text = parsed["text"] ?? "";
                voice = parsed["voice"] ?? voice;
            }
            else
            {
                text = ctx.Request.QueryString["text"] ?? "";
            }

            TextReceived?.Invoke(text);

            if (!string.IsNullOrWhiteSpace(text))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    var tt = await PiperSpeakAsync(text);
                    PlayAudio(tt);
                });
                Write(ctx, $"Received: {text}");
            }
            else
            {
                Write(ctx, "Error: Received invalid text");
                return;
            }
        }
        catch (Exception ex)
        {
            Write(ctx, $"Error: {ex}");
        }
    }

    private void Write(HttpListenerContext ctx, string msg)
    {
        using var writer = new StreamWriter(ctx.Response.OutputStream);
        writer.Write(msg);
    }

    //https://github.com/OHF-Voice/piper1-gpl/blob/main/docs/CLI.md
    //voices: https://rhasspy.github.io/piper-samples/#en_US-joe-medium
    public async Task<byte[]> PiperSpeakAsync(string text)
    {
        //python -m piper.download_voices en_US-joe-medium --download-dir "%USERPROFILE%\PiperModels"
        var modelPath = @"C:\Users\Kevin\PiperModels\en_US-joe-medium.onnx";
        var outputPath = Path.GetTempFileName() + ".wav";

        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"-m piper --model \"{modelPath}\" --output_file \"{outputPath}\"",
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        await process.StandardInput.WriteLineAsync(text);
        process.StandardInput.Close();

        await process.WaitForExitAsync();

        var bytes = File.ReadAllBytes(outputPath);
        File.Delete(outputPath);
        return bytes;
    }

    //Using window narrator for TTS, but it has very limited voice options and quality isn't great
    //public async Task<byte[]> Win11SpeakAsync(string text, string voiceName)
    //{
    //    var synth = new SpeechSynthesizer();

    //    var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.DisplayName.Contains(voiceName)) ?? SpeechSynthesizer.AllVoices.First();
    //    if (voice != null)
    //    {
    //        synth.Voice = voice;
    //    }

    //    SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);

    //    using var reader = new DataReader(stream);
    //    byte[] buffer = new byte[stream.Size];
    //    await reader.LoadAsync((uint)stream.Size);
    //    reader.ReadBytes(buffer);

    //    return buffer;
    //}

    //Azure TTS has great voice options and quality, but requires an Azure account and is paid after free tier
    //Get voice from https://speech.microsoft.com/portal/voicegallery
    //public async Task<byte[]> AzureSpeakAsync(string text, string voice)
    //{
    //    var key = _config["AzureSpeech:Key"];
    //    var region = _config["AzureSpeech:Region"];
    //    var speechConfig = SpeechConfig.FromSubscription(key, region);
    //    speechConfig.SpeechSynthesisVoiceName = voice;

    //    using var synthesizer = new SpeechSynthesizer(speechConfig, null);

    //    var result = await synthesizer.SpeakTextAsync(text);

    //    if (result.Reason != ResultReason.SynthesizingAudioCompleted)
    //    {
    //        throw new Exception("Azure TTS failed: " + result.Reason);
    //    }

    //    return result.AudioData;
    //}

    public void PlayAudio(byte[] audio)
    {
        var stream = new MemoryStream(audio);
        var random = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        var output = random.AsStreamForWrite();
        stream.CopyTo(output);
        output.Flush();
        random.Seek(0);

        var player = new MediaPlayer();
        player.Source = MediaSource.CreateFromStream(random, "audio/wav");
        player.Play();
    }
}
