using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace SpeechRelay.Host.ViewModels
{
    public class MainWindowViewModelDesign : ViewModelBase
    {
        public string LastText { get; set; } = "Waiting for text...";
        public string ListeningIp { get; set; } = "127.0.0.1";
        public string InputText { get; set; } = string.Empty;
        public string SelectedVoiceGenerator { get; set; } = "Windows Narrator";
        public List<string> AvailableVoices { get; set; } = new List<string> { "Windows Narrator", "Piper", "Azure Speech" };
        public string SelectedVoice { get; set; } = "";
        public Bitmap? PairingQrCode { get; set; }
        public string AuthToken { get; set; } = "";
        public IRelayCommand SubmitCommand => null!;
        public IRelayCommand ApplyAuthTokenCommand => null!;
        public void UpdateText(string text) { }
    }
}
