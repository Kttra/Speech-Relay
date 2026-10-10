using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using PcSpeakerApp.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using static PcSpeakerApp.Services.WebServer;

namespace PcSpeakerApp.ViewModels
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
        private string selectedVoiceGenerator = "WindowsNarrator";

        [ObservableProperty]
        private Bitmap? pairingQrCode;

        public IRelayCommand SubmitCommand { get; }
        private readonly IWebServer _webServer;
        private readonly IPairingQrCodeService _qrService;
        private readonly IConfiguration _config;

        public MainWindowViewModel(IWebServer webServer, IPairingQrCodeService qrService, IConfiguration config)
        {
            _webServer = webServer;
            _qrService = qrService;
            _config = config;

            ListeningIp = GetWifiIp();
            UpdatePairingQrCode();

            _webServer.TextReceived += text =>
            {
                LastText = text;
            };

            SubmitCommand = new RelayCommand(async () =>
            {
                LastText = InputText;
                var audio = await _webServer.ProcessVoice(InputText, "", GetDefaultAudioGenerator());
                InputText = "";
            });
        }

        private void UpdatePairingQrCode()
        {
            var ip = ListeningIp;
            var token = _config["Auth:Token"];
            var generateRandom = _config["Auth:GenerateRandomToken"]?.ToLower() == "true";
            if (generateRandom || string.IsNullOrEmpty(token))
            {
                token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            }
            _config["Auth:Token"] = token;

            var bytes = _qrService.Generate(ip, token);
            using var stream = new MemoryStream(bytes);
            PairingQrCode = new Bitmap(stream);
        }

        public void UpdateText(string text)
        {
            LastText = text;
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

        private AudioGenerator GetDefaultAudioGenerator()
        {
            if (Enum.TryParse<AudioGenerator>(SelectedVoiceGenerator ?? "", true, out var value))
            {
                return value;
            }

            return AudioGenerator.WindowsNarrator;
        }
    }
}
