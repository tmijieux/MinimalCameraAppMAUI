using System.Diagnostics;
using Android.App;
using Android.Content.PM;
using Android.Runtime;

namespace MinimalCameraApp
{


    public class CameraPermEventArgs
    {
        public bool HasPermission { get; set; }
        public bool UserInitiated { get; set; }
        public CameraPermEventArgs(bool hasPermission, bool userInitiated)
        {
            HasPermission = hasPermission;
            UserInitiated = userInitiated;
        }
    }


    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public const int CameraPermissionsCode = 107;
        public const int CameraPermissionsCode2 = 108;

        public static MainActivity Instance;
        public static event EventHandler Paused;
        public static event EventHandler Resumed;
        public static event EventHandler<CameraPermEventArgs> CameraPermissionGranted;
        public static readonly string[] CameraPermissions = {
            Android.Manifest.Permission.Camera
        };

        public MainActivity() : base()
        {
            Instance = this;
        }


        protected override void OnPause()
        {
            Paused?.Invoke(this, EventArgs.Empty);
            base.OnPause();
        }

        protected override void OnResume()
        {
            Resumed?.Invoke(this, EventArgs.Empty);
            base.OnResume();
        }
        public override void OnRequestPermissionsResult(
             int requestCode,
             string[] permissions,
             [GeneratedEnum] Permission[] grantResults)
        {
            //OnRequestPermissionsResult(
            //    requestCode, permissions, grantResults
            //);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == CameraPermissionsCode
                || requestCode == CameraPermissionsCode2)
            {
                Debug.WriteLine($"CameraPermission result = {grantResults[0]}");

                bool hasPermission = grantResults[0] == Permission.Granted;
                bool userInitiated = requestCode == CameraPermissionsCode2;
                var args = new CameraPermEventArgs(hasPermission, userInitiated);
                CameraPermissionGranted?.Invoke(this, args);
            }
        }


        protected override void OnDestroy()
        {
            Instance = null;
            Resumed = null;
            Paused = null;
            CameraPermissionGranted = null;
            // App.Current?.Quit();

            base.OnDestroy();
        }

    }
}
