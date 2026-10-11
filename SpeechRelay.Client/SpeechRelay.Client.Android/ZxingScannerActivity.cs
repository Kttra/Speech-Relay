using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using ZXing.Mobile;
using System.Threading.Tasks;
using System;

namespace SpeechRelay.Client.Platforms.Android;

[Activity(Label = "ZxingScannerActivity", Theme = "@style/MyTheme.NoActionBar")]
public class ZxingScannerActivity : Activity
{
    public static TaskCompletionSource<string?>? ResultTcs;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try
        {
            MobileBarcodeScanner.Initialize(Application);
        }
        catch
        {
            // Ignore initialization errors; scanner may still work
        }

        StartScan();
    }

    private async void StartScan()
    {
        var scanner = new MobileBarcodeScanner();
        var result = await scanner.Scan();
        ResultTcs?.TrySetResult(result?.Text);
        Finish();
    }
}
