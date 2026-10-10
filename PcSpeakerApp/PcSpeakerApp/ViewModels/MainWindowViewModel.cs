using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static PcSpeakerApp.WebServer;

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

        public IRelayCommand SubmitCommand { get; }
        private readonly IWebServer _webServer;

        public MainWindowViewModel(IWebServer webServer)
        {
            _webServer = webServer;

            ListeningIp = GetWifiIp();

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

        public void UpdateText(string text)
        {
            LastText = text;
        }

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
