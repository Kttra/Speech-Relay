using Android.App;
using Android.OS;
using Android.Content;
using Java.Lang;
using System.Threading.Tasks;
using System;

namespace SpeechRelay.Client.Platforms.Android;

[Activity(Label = "MlKitScannerActivity", Theme = "@style/MyTheme.NoActionBar")]
public class MlKitScannerActivity : Activity
{
    // Shared TaskCompletionSource used by the platform scanner wrapper.
    public static TaskCompletionSource<string?>? ResultTcs;

    const int ScanRequestCode = 10042;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try
        {
            // Try to find the ML Kit CodeScanner activity class from the binding.
            // The Java class is typically "com.google.mlkit.vision.codescanner.CodeScannerActivity".
            var scannerClass = Class.ForName("com.google.mlkit.vision.codescanner.CodeScannerActivity");

            var intent = new Intent(this, scannerClass);

            // Try to restrict scanning to QR codes using common extras used by the library.
            // These extras are best-effort; the activity will ignore unknown extras.
            intent.PutExtra("com.google.mlkit.vision.codescanner.EXTRA_ONLY_QR", true);
            intent.PutExtra("com.google.mlkit.vision.codescanner.EXTRA_FORMATS", new int[] { 256 });
            intent.PutExtra("com.google.mlkit.vision.codescanner.EXTRA_CODE_TYPES", new int[] { 256 });

            StartActivityForResult(intent, ScanRequestCode);
        }
        catch (ClassNotFoundException)
        {
            // Binding/activity not available at runtime — return null so caller can handle.
            ResultTcs?.TrySetResult(null);
            Finish();
        }
        catch (global::System.Exception)
        {
            // Any unexpected error should result in a null result to the caller.
            ResultTcs?.TrySetResult(null);
            Finish();
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode != ScanRequestCode)
            return;

        try
        {
            if (resultCode == Result.Ok && data != null)
            {
                string? text = null;

                // Try a few well-known extra keys, then fall back to scanning extras for the first string value.
                var keysToTry = new[] {
                    "com.google.mlkit.vision.codescanner.RESULT",
                    "com.google.mlkit.vision.codescanner.EXTRA_TEXT",
                    "com.google.mlkit.codescanner.RESULT",
                    "SCAN_RESULT",
                    Intent.ExtraText
                };

                foreach (var k in keysToTry)
                {
                    if (data.HasExtra(k))
                    {
                        text = data.GetStringExtra(k);
                        if (!string.IsNullOrEmpty(text)) break;
                    }
                }

                if (string.IsNullOrEmpty(text) && data.Extras != null)
                {
                    foreach (var key in data.Extras.KeySet())
                    {
                        var s = data.GetStringExtra(key);
                        if (!string.IsNullOrEmpty(s))
                        {
                            text = s;
                            break;
                        }
                    }
                }

                ResultTcs?.TrySetResult(text);
            }
            else
            {
                // Cancelled or failed
                ResultTcs?.TrySetResult(null);
            }
        }
        catch
        {
            ResultTcs?.TrySetResult(null);
        }

        Finish();
    }
}
