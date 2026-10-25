using SpeechRelay.Shared.Models;
using System.Collections.Generic;

namespace SpeechRelay.Shared.Services
{
    public static class VoiceOptions
    {
        /// <summary>
        /// Returns a list of voice names available for the specified generator.
        /// </summary>
        public static List<string> GetVoices(AudioGenerator generator)
        {
            return generator switch
            {
                AudioGenerator.WindowsNarrator => new List<string>
                {
                    "Microsoft David",
                    "Microsoft Zira",
                    "Microsoft Mark",
                    "Microsoft Aria",
                    "Microsoft Jenny",
                    "Microsoft Guy"
                },
                AudioGenerator.AzureSpeech => new List<string>
                {
                    "Phoebe",
                    "Serena",
                    "Onyx"
                },
                AudioGenerator.Piper => new List<string> { "Joe" },
                _ => new List<string>()
            };
        }

        /// <summary>
        /// Maps the public generator name string to the enum value. String comparison is case‑insensitive and ignores spaces.
        /// </summary>
        public static AudioGenerator GetAudioGenerator(string generatorName)
        {
            if (string.IsNullOrEmpty(generatorName))
            {
                return AudioGenerator.WindowsNarrator;
            }

            var key = generatorName.Trim().Replace(" ", "").ToLowerInvariant();
            return key switch
            {
                "windowsnarrator" => AudioGenerator.WindowsNarrator,
                "azurespeech" => AudioGenerator.AzureSpeech,
                "piper" => AudioGenerator.Piper,
                _ => AudioGenerator.WindowsNarrator,
            };
        }

        /// <summary>
        /// Maps a user‑selected voice name to the full TTS voice spec used by the server.
        /// </summary>
        public static string GetVoiceName(string voice)
        {
            return voice switch
            {
                //Azure Speech voices
                var v when v.StartsWith("Phoebe", System.StringComparison.OrdinalIgnoreCase) => "en-US-Phoebe:DragonHDLatestNeural",
                var v when v.StartsWith("Serena", System.StringComparison.OrdinalIgnoreCase) => "en-US-Serena:DragonHDLatestNeural",
                var v when v.StartsWith("Onyx", System.StringComparison.OrdinalIgnoreCase) => "en-US-OnyxTurboMultilingualNeural",

                //Piper voices
                var v when v.StartsWith("Joe", System.StringComparison.OrdinalIgnoreCase) => "en_US-joe-medium",

                //Other voices
                _ => voice
            };
        }
    }
}
