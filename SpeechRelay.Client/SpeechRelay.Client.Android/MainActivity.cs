using Avalonia.Android;
using global::Android.App;
using global::Android.Content.PM;

namespace SpeechRelay.Client.Platforms.Android
{
    [Activity(Label = "Speech Relay Client",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity
    {
        public static MainActivity? Instance { get; private set; }
        private const int RequestCameraPermissionCode = 2001;
        private static System.Threading.Tasks.TaskCompletionSource<bool>? _permissionTcs;
        private const int RequestImageCapture = 1001;
        private static System.Threading.Tasks.TaskCompletionSource<global::Android.Graphics.Bitmap?>? _tcs;

        protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Instance = this;
        }

        public static System.Threading.Tasks.Task<bool> RequestCameraPermissionAsync()
        {
            if (Instance == null) return System.Threading.Tasks.Task.FromResult(false);

            var ctx = Instance;
            if (global::AndroidX.Core.Content.ContextCompat.CheckSelfPermission(ctx, global::Android.Manifest.Permission.Camera) == (int)global::Android.Content.PM.Permission.Granted)
            {
                return System.Threading.Tasks.Task.FromResult(true);
            }

            _permissionTcs = new System.Threading.Tasks.TaskCompletionSource<bool>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
            ctx.RequestPermissions(new[] { global::Android.Manifest.Permission.Camera }, RequestCameraPermissionCode);
            return _permissionTcs.Task;
        }

        public static System.Threading.Tasks.Task<global::Android.Graphics.Bitmap?> TakePictureAsync()
        {
            if (Instance == null) return System.Threading.Tasks.Task.FromResult<global::Android.Graphics.Bitmap?>(null);

            _tcs = new System.Threading.Tasks.TaskCompletionSource<global::Android.Graphics.Bitmap?>();
            var intent = new global::Android.Content.Intent(global::Android.Provider.MediaStore.ActionImageCapture);
            Instance.StartActivityForResult(intent, RequestImageCapture);
            return _tcs.Task;
        }

        protected override void OnActivityResult(int requestCode, global::Android.App.Result resultCode, global::Android.Content.Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == RequestImageCapture)
            {
                global::Android.Graphics.Bitmap? bmp = null;
                if (resultCode == global::Android.App.Result.Ok && data != null)
                {
                    bmp = data.Extras?.Get("data") as global::Android.Graphics.Bitmap;
                }

                _tcs?.TrySetResult(bmp);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == RequestCameraPermissionCode)
            {
                var granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
                _permissionTcs?.TrySetResult(granted);
                _permissionTcs = null;
            }
        }
    }
}
