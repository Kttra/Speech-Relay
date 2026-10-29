using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using SpeechRelay.Client.Services;
using SpeechRelay.Shared.Constants;
using SpeechRelay.Shared.Models;
using SpeechRelay.Shared.Services;
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

            OnSelectedVoiceGeneratorChanged(SelectedVoiceGenerator);
            if (!string.IsNullOrEmpty(_settings.SelectedVoice) && AvailableVoices.Contains(_settings.SelectedVoice))
            {
                SelectedVoice = _settings.SelectedVoice;
            }
            IsAndroid = OperatingSystem.IsAndroid();
            AuthToken = string.IsNullOrEmpty(_settings.AuthToken) ? (config["Api:AuthToken"] ?? string.Empty) : _settings.AuthToken;
        }

        partial void OnSelectedVoiceGeneratorChanged(string value)
        {
            var generator = VoiceOptions.GetAudioGenerator(value);
            AvailableVoices = VoiceOptions.GetVoices(generator);
            SelectedVoice = !string.IsNullOrEmpty(SelectedVoice) && AvailableVoices.Contains(SelectedVoice)
                ? SelectedVoice
                : (AvailableVoices.Count > 0 ? AvailableVoices[0] : string.Empty);
        }

        private void SaveSettings()
        {
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

                var url = $"http://{PcIp}:{SpeechRelayNetwork.DefaultPort}{SpeechRelayNetwork.SpeakRoute}";
                var request = new SpeechRequest
                {
                    Text = msg,
                    Voice = VoiceOptions.GetVoiceName(SelectedVoice),
                    Generator = VoiceOptions.GetAudioGenerator(SelectedVoiceGenerator)
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
