using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using SpeechRelay.Host.Services;
using SpeechRelay.Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace SpeechRelay.Host.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string lastText = "Waiting for text...";

        [ObservableProperty]
        private string listeningIp = "";

        [ObservableProperty]
        private string inputText = "";

        [ObservableProperty]
        private string selectedVoiceGenerator = "";

        [ObservableProperty]
        private List<string> availableVoices = new();

        [ObservableProperty]
        private string selectedVoice = string.Empty;

        [ObservableProperty]
        private Bitmap? pairingQrCode;

        [ObservableProperty]
        private string authToken = "";

        public IRelayCommand SubmitCommand { get; }
        public IRelayCommand ApplyAuthTokenCommand { get; }
        private readonly IWebServer _webServer;
        private readonly IPairingQrCodeService _qrService;
        private readonly IConfiguration _config;

        public MainWindowViewModel(IWebServer webServer, IPairingQrCodeService qrService, IConfiguration config)
        {
            _webServer = webServer;
            _qrService = qrService;
            _config = config;

            SelectedVoiceGenerator = _config["DefaultAudioGenerator"] ?? "Windows Narrator";
            var initialGenerator = VoiceOptions.GetAudioGenerator(SelectedVoiceGenerator);
            AvailableVoices = VoiceOptions.GetVoices(initialGenerator);
            SelectedVoice = AvailableVoices.Count > 0 ? AvailableVoices[0] : string.Empty;
            ListeningIp = GetWifiIp();
            UpdatePairingQrCode();

            _webServer.TextReceived += text =>
            {
                SetLastText(text);
            };

            SubmitCommand = new RelayCommand(async () =>
            {
                SetLastText(InputText);
                var audio = await _webServer.ProcessVoice(InputText, VoiceOptions.GetVoiceName(SelectedVoice), VoiceOptions.GetAudioGenerator(SelectedVoiceGenerator));
                InputText = "";
            });

            ApplyAuthTokenCommand = new RelayCommand(() =>
            {
                if (string.IsNullOrEmpty(AuthToken))
                    return;
                _config["Auth:Token"] = AuthToken;
                UpdatePairingQrCode();
            });
        }

        partial void OnSelectedVoiceGeneratorChanged(string value)
        {
            var generator = VoiceOptions.GetAudioGenerator(value);
            AvailableVoices = VoiceOptions.GetVoices(generator);

            // Default to the first available voice, if any.
            SelectedVoice = AvailableVoices.Count > 0 ? AvailableVoices[0] : string.Empty;
        }

        private void UpdatePairingQrCode()
        {
            var ip = ListeningIp;
            var token = _config["Auth:Token"] ?? string.Empty;
            var generateRandom = _config["Auth:GenerateRandomToken"]?.ToLower() == "true";
            if (generateRandom || string.IsNullOrEmpty(token))
            {
                token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            }
            _config["Auth:Token"] = token;

            AuthToken = token;
            var bytes = _qrService.Generate(ip, token);
            using var stream = new MemoryStream(bytes);
            PairingQrCode = new Bitmap(stream);
        }

        private void SetLastText(string text)
        {
            LastText = $"{text} (Voice: {SelectedVoice}, Generator: {SelectedVoiceGenerator})";
        }

        // May need to use a different method to get the IP address in the future, as this method may not work in all environments
        private static string GetWifiIp()
        {
            var wifi = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && n.OperationalStatus == OperationalStatus.Up);

            if (wifi == null)
            {
                return "127.0.0.1";
            }

            return wifi.GetIPProperties()
                .UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                ?.Address.ToString() ?? "127.0.0.1";
        }
    }
}
