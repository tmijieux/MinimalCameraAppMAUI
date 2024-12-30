using Android;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform;
using MinimalCameraApp.Controls;

namespace MinimalCameraApp.Platform.Renderers.Camera
{
    public class CameraViewRenderer : ViewRenderer<CameraView, CameraDroid>
    {
        private readonly Context _context;
        private CameraOptions _cameraOption;
        private CameraDroid _cameraDroid;

        public CameraViewRenderer(Context context) : base(context)
        {
            _context = context;
        }

        private TaskCompletionSource<MemoryStream> _tcsPhoto;
        private void OnPhoto(object sender, MemoryStream imStream)
        {
            _cameraDroid.Photo -= OnPhoto;
            if (_tcsPhoto == null)
                return;
            var tcs = _tcsPhoto;
            _tcsPhoto = null;
            tcs.SetResult(imStream);
        }

        private void OnPhotoRequest(
            object sender, TaskCompletionSource<MemoryStream> tcs)
        {
            _tcsPhoto = tcs;
            _cameraDroid.Photo += OnPhoto;
            _cameraDroid.TakePhoto();
        }

        private void OnPermissionGranted(object sender, CameraPermEventArgs e)
        {
            if (!e.HasPermission && e.UserInitiated)
            {
                bool showRationale = MainActivity
                    .Instance
                    .ShouldShowRequestPermissionRationale(
                        MainActivity.CameraPermissions[0]
                    );
                if (!showRationale)
                {
                    // user has most probably checked "do not show again".
                    // Redirect the user to the app config page
                    AppInfo.ShowSettingsUI();
                }
            }
            else if (e.HasPermission)
            {
                Element.HasPermission = true;
                SetupCamera();
            }
        }

        private void OnPermissionRequest(object _1, object _2)
        {
            MainActivity.CameraPermissionGranted -= OnPermissionGranted;
            MainActivity.CameraPermissionGranted += OnPermissionGranted;

            var (hasPermission,_) = RequestCameraPermissionIfRequired(userInitiated: true);
            if (hasPermission)
            { // already had permission
                MainActivity.CameraPermissionGranted -= OnPermissionGranted;
                return;
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement != null)
            {
                _cameraDroid.Dispose();
                e.OldElement.PhotoRequest -= OnPhotoRequest;
            }

            if (e.NewElement != null)
            {
                var elem = e.NewElement;
                if (Control == null)
                {
                    System.Diagnostics.Debug.WriteLine("control is null setting up new element");
                    _cameraOption = elem.Camera;
                    MainActivity.CameraPermissionGranted -= OnPermissionGranted;
                    MainActivity.CameraPermissionGranted += OnPermissionGranted;
                    var (havePermission,wasRequired) = RequestCameraPermissionIfRequired();
                    if (havePermission)
                    {
                        System.Diagnostics.Debug.WriteLine("have permission");

                        MainActivity.CameraPermissionGranted -= OnPermissionGranted;
                        if (wasRequired)
                        {
                            Element.HasPermission = true;
                            SetupCamera();
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine("waiting for permission setup...");
                                    await Task.Delay(TimeSpan.FromSeconds(1));
                                    System.Diagnostics.Debug.WriteLine("waiting finished...");

                                    SetupCamera();
                                }
                                catch (Exception ex)
                                {
                                }
                            });
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("dont have permission");
                        elem.HasPermission = false;
                    }
                }
                elem.PhotoRequest -= OnPhotoRequest;
                elem.PhotoRequest += OnPhotoRequest;
                elem.PermissionRequest -= OnPermissionRequest;
                elem.PermissionRequest += OnPermissionRequest;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                MainActivity.CameraPermissionGranted -= OnPermissionGranted;
            }
            base.Dispose(disposing);
        }

        private void SetupCamera()
        {
            System.Diagnostics.Debug.WriteLine("Begin setup camera!");
            _cameraDroid = new CameraDroid(Context);
            _cameraDroid.OpenCamera(_cameraOption);
            SetNativeControl(_cameraDroid);
        }

        private bool HaveCameraPermissions()
        {
            const string permission = Manifest.Permission.Camera;
            if ((int)Build.VERSION.SdkInt < 23
                || ContextCompat.CheckSelfPermission(
                    Android.App.Application.Context, permission) == Permission.Granted)
            {
                return true;
            }
            return false;
        }

        private (bool,bool) RequestCameraPermissionIfRequired(bool userInitiated = false)
        {
            bool havePermission = HaveCameraPermissions();
            if (!havePermission)
            {
                ActivityCompat.RequestPermissions(
                    (MainActivity)_context,
                    MainActivity.CameraPermissions,
                    (
                        userInitiated
                        ? MainActivity.CameraPermissionsCode2
                        : MainActivity.CameraPermissionsCode
                    )
                );
                return (havePermission, true);
            }
            return (true,false);
        }
    }
}
