using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using SpeechRelay.Client.Services;
using SpeechRelay.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SpeechRelay.Client.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly HttpClient _http;
        private readonly SettingsService _settingsService;
        private readonly AppSettings _settings;
        private readonly IQRCodeScanner _qrScanner;

        [ObservableProperty]
        private string pcIp;
        [ObservableProperty]
        private string commandText = "";
        [ObservableProperty]
        private string serverResponse = "";
        [ObservableProperty]
        private string selectedVoice = "";
        [ObservableProperty]
        private string selectedPresetMsg = "Custom";
        [ObservableProperty]
        private string selectedVoiceGenerator = "Windows Narrator";
        [ObservableProperty]
        private List<string> availableVoices = new();
        [ObservableProperty]
        private string authToken = "";
        [ObservableProperty]
        private bool isAndroid = false;

        public MainViewModel(HttpClient http, IConfiguration config, SettingsService settingsService, IQRCodeScanner qrScanner)
        {
            _http = http;
            _settingsService = settingsService;
            _qrScanner = qrScanner;
            _settings = settingsService.Load();

            var defaultPcIp = config["Api:DefaultPcIp"] ?? "";
            PcIp = string.IsNullOrEmpty(_settings.PcIp) ? defaultPcIp : _settings.PcIp;
            SelectedVoiceGenerator = _settings.SelectedVoiceGenerator;
            if (AvailableVoices.Contains(_settings.SelectedVoice))
            {
                SelectedVoice = _settings.SelectedVoice;
            }

            OnSelectedVoiceGeneratorChanged(SelectedVoiceGenerator);
            IsAndroid = OperatingSystem.IsAndroid();
            AuthToken = string.IsNullOrEmpty(_settings.AuthToken) ? (config["Api:AuthToken"] ?? string.Empty) : _settings.AuthToken;
        }

        partial void OnSelectedVoiceGeneratorChanged(string value)
        {
            AvailableVoices = value switch
            {
                "Windows Narrator" => new List<string>
                {
                    "Microsoft David",
                    "Microsoft Zira",
                    "Microsoft Mark",
                    "Microsoft Aria",
                    "Microsoft Jenny",
                    "Microsoft Guy"
                },

                "Azure Speech" => new List<string>
                {
                    "Phoebe",
                    "Serena",
                    "Onyx"
                },

                "Piper" => new List<string>
                {
                    "Joe"
                },

                _ => new List<string>()
            };

            SelectedVoice = AvailableVoices.Count > 0 ? AvailableVoices[0] : string.Empty;
        }

        private string GetVoiceName()
        {
            return SelectedVoice switch
            {
                var v when v.StartsWith("Phoebe", StringComparison.OrdinalIgnoreCase)
                    => "en-US-Phoebe:DragonHDLatestNeural",
                var v when v.StartsWith("Serena", StringComparison.OrdinalIgnoreCase)
                    => "en-US-Serena:DragonHDLatestNeural",
                var v when v.StartsWith("Onyx", StringComparison.OrdinalIgnoreCase)
                    => "en-US-OnyxTurboMultilingualNeural",
                var v when v.StartsWith("Joe", StringComparison.OrdinalIgnoreCase)
                    => "en_US-joe-medium",
                _ => SelectedVoice
            };
        }

        private AudioGenerator GetGeneratorName()
        {
            return SelectedVoiceGenerator switch
            {
                "Windows Narrator" => AudioGenerator.WindowsNarrator,
                "Azure Speech" => AudioGenerator.AzureSpeech,
                "Piper" => AudioGenerator.Piper,
                _ => AudioGenerator.WindowsNarrator
            };
        }

        private void SaveSettings()
        {
            if (_settingsService == null || _settings == null)
                return;

            _settings.PcIp = PcIp;
            _settings.SelectedVoice = SelectedVoice;
            _settings.SelectedVoiceGenerator = SelectedVoiceGenerator;
            _settings.AuthToken = AuthToken ?? string.Empty;

            _settingsService.Save(_settings);
        }

        [RelayCommand]
        private async Task SendAsync()
        {
            var isPresetMsg = SelectedPresetMsg != "Custom";
            try
            {
                var msg = isPresetMsg ? SelectedPresetMsg : CommandText;
                if (string.IsNullOrWhiteSpace(PcIp) || string.IsNullOrWhiteSpace(msg))
                {
                    ServerResponse = "Cannot send empty message";
                    return;
                }

                var url = $"http://{PcIp}:5000/speak/";
                var request = new SpeechRequest
                {
                    Text = msg,
                    Voice = GetVoiceName(),
                    Generator = GetGeneratorName()
                };
                var response = await _http.PostAsJsonAsync(url, request, SpeechRelayJson.Options);
                ServerResponse = await response.Content.ReadAsStringAsync();

                // Save the current settings after a successful send
                SaveSettings();
            }
            catch (Exception ex)
            {
                ServerResponse = "Error sending request: " + ex.Message;
            }

            //Reset command text or preset message after sending
            if (isPresetMsg)
            {
                SelectedPresetMsg = "Custom";
            }
            else
            {
                CommandText = string.Empty;
            }
        }

        [RelayCommand]
        private async Task ScanPairingFromCameraAsync()
        {
            try
            {
                var payload = await _qrScanner.ScanFromCameraAsync();
                if (payload == null)
                {
                    ServerResponse = "No pairing data found.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(payload.IpAddress))
                {
                    ServerResponse = "Invalid pairing data.";
                    return;
                }

                // Apply pairing payload
                PcIp = payload.IpAddress;
                if (!string.IsNullOrEmpty(payload.Token))
                {
                    _settings.AuthToken = payload.Token;
                    AuthToken = payload.Token;
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Token);
                }

                SaveSettings();
                ServerResponse = $"Paired with {PcIp}";
            }
            catch (Exception ex)
            {
                ServerResponse = "Error scanning pairing data: " + ex.Message;
            }
        }
    }
}
