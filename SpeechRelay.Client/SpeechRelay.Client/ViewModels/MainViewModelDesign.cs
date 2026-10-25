using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace SpeechRelay.Client.ViewModels
{
    public class MainViewModelDesign : ViewModelBase
    {
        public string PcIp { get; set; } = "127.0.0.1";
        public string CommandText { get; set; } = string.Empty;
        public string ServerResponse { get; set; } = string.Empty;
        public string SelectedVoice { get; set; } = "";
        public string SelectedPresetMsg { get; set; } = "Custom";
        public string SelectedVoiceGenerator { get; set; } = "Windows Narrator";
        public List<string> AvailableVoices { get; set; } = new List<string> { "Windows Narrator", "Azure Speech", "Piper" };
        public string AuthToken { get; set; } = string.Empty;
        public bool IsAndroid { get; set; } = false;

        public IRelayCommand SendCommand => null!;
        public IRelayCommand ScanPairingFromCameraCommand => null!;
    }
}
