using Android.Content;
using SpeechRelay.Client.Services;
using SpeechRelay.Shared.Models;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpeechRelay.Client.Platforms.Android;

public class MlKitBarcodeScanner : IQRCodeScanner
{
    public async Task<PairingPayload?> ScanFromCameraAsync()
    {
        var intent = new Intent(global::Android.App.Application.Context, typeof(MlKitScannerActivity));
        MlKitScannerActivity.ResultTcs = new TaskCompletionSource<string?>();
        intent.AddFlags(ActivityFlags.NewTask);
        global::Android.App.Application.Context.StartActivity(intent);

        var result = await MlKitScannerActivity.ResultTcs.Task;
        if (string.IsNullOrEmpty(result)) return null;

        try
        {
            var payload = JsonSerializer.Deserialize<PairingPayload>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return payload;
        }
        catch
        {
            return null;
        }
    }
}
