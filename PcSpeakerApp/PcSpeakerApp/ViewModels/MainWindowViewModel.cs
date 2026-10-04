using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PcSpeakerApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string lastText = "Waiting for text...";

        [ObservableProperty]
        private string inputText = "";

        public IRelayCommand SubmitCommand { get; }
        private readonly IWebServer _webServer;

        public MainWindowViewModel(IWebServer webServer)
        {
            _webServer = webServer;

            _webServer.TextReceived += text =>
            {
                LastText = text;
            };

            SubmitCommand = new RelayCommand(async () =>
            {
                LastText = InputText;
                var audio = await _webServer.PiperSpeakAsync(InputText);
                _webServer.PlayAudio(audio);
                InputText = "";
            });
        }

        public void UpdateText(string text)
        {
            LastText = text;
        }
    }
}
