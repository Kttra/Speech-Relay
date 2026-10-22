using Microsoft.Extensions.Configuration;
using SpeechRelay.Shared.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using AzureSpeech = Microsoft.CognitiveServices.Speech;

namespace SpeechRelay.Host.Services;

public class WebServer : IWebServer
{
    public event Action<string>? TextReceived;
    private readonly IConfiguration _config;

    public WebServer(IConfiguration config)
    {
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
            // Expect Authorization: Bearer <token>
            var authHeader = ctx.Request.Headers["Authorization"];
            string providedToken = "";
            if (!string.IsNullOrEmpty(authHeader))
            {
                const string bearer = "Bearer ";
                if (authHeader.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
                {
                    providedToken = authHeader.Substring(bearer.Length).Trim();
                }
                else
                {
                    providedToken = authHeader;
                }
            }

            var expectedToken = _config["Auth:Token"];
            if (providedToken != expectedToken)
            {
                ctx.Response.StatusCode = 401;
                Write(ctx, "Unauthorized");
                return;
            }

            var text = "";
            var voice = "en-US-OnyxTurboMultilingualNeural";
            var generator = AudioGenerator.WindowsNarrator;

            if (ctx.Request.HttpMethod == "POST")
            {
                try
                {
                    var request = await JsonSerializer.DeserializeAsync<SpeechRequest>(
                        ctx.Request.InputStream,
                        SpeechRelayJson.Options);

                    if (request == null)
                    {
                        ctx.Response.StatusCode = 400;
                        return;
                    }

                    text = request.Text ?? "";
                    voice = string.IsNullOrEmpty(request.Voice) ? voice : request.Voice;
                    generator = request.Generator;
                }
                catch (JsonException)
                {
                    ctx.Response.StatusCode = 400;
                    return;
                }
            }
            else
            {
                text = ctx.Request.QueryString["text"] ?? "";
            }

            TextReceived?.Invoke(text);

            if (!string.IsNullOrWhiteSpace(text))
            {
                await ProcessVoice(text, voice, generator);
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

    public async Task<bool> ProcessVoice(string text, string voice, AudioGenerator audioGenerator)
    {
        byte[]? audio = null;
        switch (audioGenerator)
        {
            case AudioGenerator.Piper:
                audio = await PiperSpeakAsync(text);
                break;
            case AudioGenerator.AzureSpeech:
                audio = await AzureSpeakAsync(text, voice);
                break;
            case AudioGenerator.WindowsNarrator:
            default:
                audio = await Win11SpeakAsync(text, voice);
                break;
        }

        if (audio == null)
        {
            return false;
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            PlayAudio(audio);
        });
        return true;
    }

    private void Write(HttpListenerContext ctx, string msg)
    {
        using var writer = new StreamWriter(ctx.Response.OutputStream);
        writer.Write(msg);
    }

    //Piper: https://github.com/OHF-Voice/piper1-gpl/blob/main/docs/CLI.md
    //Voice: https://rhasspy.github.io/piper-samples/#en_US-joe-medium
    //Download voice command: python -m piper.download_voices en_US-joe-medium --download-dir "%USERPROFILE%\PiperModels"
    public async Task<byte[]> PiperSpeakAsync(string text, string voice = "en_US-joe-medium.onnx")
    {
        var modelPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "PiperModels",
            voice
        );

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("Piper voice model not found.", modelPath);
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "py",
                Arguments = $"-3.12 -m piper --model \"{modelPath}\" --output_file \"{outputPath}\"",
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start Piper.");
            var errorTask = process.StandardError.ReadToEndAsync();

            try
            {
                await process.StandardInput.WriteLineAsync(text);
            }
            catch (IOException)
            {
                // Piper may have exited before accepting input.
            }
            finally
            {
                process.StandardInput.Close();
            }

            await process.WaitForExitAsync();
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Piper failed (exit code {process.ExitCode}): {error}");
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException("Piper did not generate an audio file.", outputPath);
            }
            return await File.ReadAllBytesAsync(outputPath);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    //Using window narrator for TTS, but it has very limited voice options and quality isn't great
    public async Task<byte[]> Win11SpeakAsync(string text, string voiceName)
    {
        var synth = new SpeechSynthesizer();

        var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.DisplayName.Contains(voiceName)) ?? SpeechSynthesizer.AllVoices.First();
        if (voice != null)
        {
            synth.Voice = voice;
        }

        SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);
        using var reader = new DataReader(stream);
        byte[] buffer = new byte[stream.Size];
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(buffer);

        return buffer;
    }

    //Azure TTS has great voice options and quality, but requires an Azure account and is paid after free tier
    //Get voice from https://speech.microsoft.com/portal/voicegallery
    public async Task<byte[]> AzureSpeakAsync(string text, string voice)
    {
        var key = _config["AzureSpeech:Key"];
        var region = _config["AzureSpeech:Region"];

        var speechConfig = AzureSpeech.SpeechConfig.FromSubscription(key, region);
        speechConfig.SpeechSynthesisVoiceName = voice;

        using var synthesizer = new AzureSpeech.SpeechSynthesizer(speechConfig, null);

        var result = await synthesizer.SpeakTextAsync(text);

        if (result.Reason != AzureSpeech.ResultReason.SynthesizingAudioCompleted)
        {
            throw new Exception("Azure TTS failed: " + result.Reason);
        }

        return result.AudioData;
    }

    public void PlayAudio(byte[] audio)
    {
        if (audio == null)
        {
            return;
        }

        var stream = new MemoryStream(audio);
        var random = new InMemoryRandomAccessStream();
        var output = random.AsStreamForWrite();
        stream.CopyTo(output);
        output.Flush();
        random.Seek(0);

        var player = new MediaPlayer();
        player.Source = MediaSource.CreateFromStream(random, "audio/wav");
        player.Play();
    }
}
