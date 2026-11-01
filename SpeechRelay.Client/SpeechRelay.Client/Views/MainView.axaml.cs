using Avalonia.Controls;
using Avalonia.Input;
using SpeechRelay.Client.ViewModels;

namespace SpeechRelay.Client.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SendCommand.Execute(null);
                }

                // Consume the key event and move focus away to close the virtual keyboard
                e.Handled = true;
                // Move focus to Send button to hide keyboard
                this.FindControl<Button>("SendButton")?.Focus();
            }
        }
    }
}