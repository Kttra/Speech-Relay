using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhoneSpeaker.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly HttpClient _http;
        [ObservableProperty]
        private string pcIp;
        [ObservableProperty]
        private string commandText = "";
        [ObservableProperty]
        private string serverResponse = "";
        [ObservableProperty]
        private string selectedVoice = "Onyx Turbo Multilingual";
        [ObservableProperty]
        private string selectedPresetMsg = "Custom";

        public MainViewModel(HttpClient http, IConfiguration config)
        {
            _http = http;
            PcIp = config["Api:DefaultPcIp"] ?? "";
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
                _ => "en-US-OnyxTurboMultilingualNeural"
            };
        }

        private bool IsPresetMsg()
        {
            return SelectedPresetMsg != "Custom";
        }

        //Send button
        [RelayCommand]
        private async Task SendAsync()
        {
            var isPresetMsg = IsPresetMsg();
            try
            {
                var msg = isPresetMsg ? SelectedPresetMsg : CommandText;
                if (string.IsNullOrWhiteSpace(PcIp) || string.IsNullOrWhiteSpace(msg))
                {
                    ServerResponse = "Cannot send empty message";
                    return;
                }

                var url = $"http://{PcIp}:5000/speak/";

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("text", msg),
                    new KeyValuePair<string, string>("voice", GetVoiceName())
                });

                var response = await _http.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                ServerResponse = body;
            }
            catch (Exception ex)
            {
                ServerResponse = "Error sending request: " + ex.Message;
            }

            //Reset command text and preset message after sending
            if (isPresetMsg)
            {
                SelectedPresetMsg = "Custom";
            }
            else
            {
                CommandText = string.Empty;
            }
        }
    }
}
