using global::Android.Gms.Tasks;
using global::Android.Runtime;
using SpeechRelay.Client.Services;
using SpeechRelay.Shared.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.CodeScanner;

namespace SpeechRelay.Client.Platforms.Android;

public class MlKitBarcodeScanner : IQRCodeScanner
{
    public async Task<PairingPayload?> ScanFromCameraAsync()
    {
        var activity = MainActivity.Instance ?? throw new InvalidOperationException("MainActivity is not available.");
        var options = new GmsBarcodeScannerOptions.Builder()
            .SetBarcodeFormats(Barcode.FormatQrCode)
            .Build();
        var scanner = GmsBarcodeScanning.GetClient(activity, options);
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var listener = new ScanListener(completion);
        var scanTask = scanner.StartScan();
        scanTask.AddOnCompleteListener(listener);

        var text = await completion.Task;
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PairingPayload>(
                text,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class ScanListener(
        TaskCompletionSource<string?> completion)
        : Java.Lang.Object, IOnCompleteListener
    {
        public void OnComplete(global::Android.Gms.Tasks.Task task)
        {
            if (task.IsCanceled)
            {
                completion.TrySetResult(null);
            }
            else if (task.IsSuccessful)
            {
                var barcode = task.Result?.JavaCast<Barcode>();
                completion.TrySetResult(barcode?.RawValue);
            }
            else
            {
                completion.TrySetException(new InvalidOperationException(task.Exception?.Message ?? "QR scanning failed."));
            }
        }
    }
}