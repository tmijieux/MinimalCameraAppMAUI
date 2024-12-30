using System.Diagnostics;
using AVFoundation;
using Foundation;
using Microsoft.Maui.Handlers;
using MinimalCameraApp.Controls;
using UIKit;


namespace MinimalCameraApp.Platform.Renderers.Camera
{
    public class CameraViewHandler : ViewHandler<CameraView, UICameraView>
    {
        private UICameraView _uiCameraView;
        //private CompositeDisposable _disposables = new();

        private static IPropertyMapper PropertyMapper = new PropertyMapper<CameraView, CameraViewHandler>
        {

        };
        private static CommandMapper CommandMapper = new CommandMapper<CameraView, CameraViewHandler>
        {
            //[nameof(CameraView.PermissionRequest)] = MapRequestPermission,
            //[nameof(CameraView.PhotoRequest)] = MapPhotoRequest,
        };
        private async Task<bool> RequestPermission(bool userInitiated)
        {
            bool hasPermission = false;

            var mediaType = AVAuthorizationMediaType.Video;
            var cameraAuthStatus = AVCaptureDevice.GetAuthorizationStatus(mediaType);
            switch (cameraAuthStatus)
            {
                case AVAuthorizationStatus.NotDetermined:
                    Debug.WriteLine("Asking camera permission");
                    var tcs = new TaskCompletionSource<bool>();
                    AVCaptureDevice.RequestAccessForMediaType(
                        AVAuthorizationMediaType.Video,
                        granted =>
                        {
                            if (granted)
                            {
                                Device.BeginInvokeOnMainThread(() => VirtualView.HasPermission = true);
                            }
                            tcs.SetResult(granted);
                        });
                    hasPermission = await tcs.Task;
                    break;
                case AVAuthorizationStatus.Restricted:
                    Debug.WriteLine("Camera restricted");
                    hasPermission = false;
                    break;
                case AVAuthorizationStatus.Denied:
                    Debug.WriteLine("Camera denied");
                    VirtualView.HasPermission = false;
                    if (userInitiated)
                    {
                        var url = new NSUrl(UIApplication.OpenSettingsUrlString);
                        UIApplication.SharedApplication.OpenUrl(url);
                    }
                    break;
                case AVAuthorizationStatus.Authorized:
                    Debug.WriteLine("Camera authorized !! ");
                    hasPermission = true;
                    VirtualView.HasPermission = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return hasPermission;
        }

        private static void MapCommandRequestPermission(
            CameraViewHandler handler, CameraView view, object arg)
        {
            handler.RequestAndLaunch(true);
        }

        private void MapRequestPermissionEventHandler(object source, EventArgs e)
        {
            MapCommandRequestPermission(this, VirtualView, null);
        }

        private async void RequestAndLaunch(bool userInitiated)
        {
            bool hasPermission = false;
            try
            {
                hasPermission = await RequestPermission(userInitiated);
            }
            catch (Exception ex)
            {
                hasPermission = false;
            }

            if (hasPermission)
            {
                PlatformView.Initialize();
            }
        }

        public CameraViewHandler() : base(PropertyMapper, CommandMapper)
        {
            //App.ScreenUpdated
            //    .ObserveOn(RxApp.MainThreadScheduler)
            //    .Subscribe(_ =>
            //    {
            //        if (PlatformView != null)
            //        {
            //            PlatformView.SetupOrientation();
            //        }
            //    })
            //    .DisposeWith(_disposables);
        }

        protected override void ConnectHandler(UICameraView platformView)
        {
            RequestAndLaunch(false);
            VirtualView.PermissionRequest += MapRequestPermissionEventHandler;
            VirtualView.PhotoRequest += OnPhotoRequest;
        }

        protected override void DisconnectHandler(UICameraView platformView)
        {
            Debug.WriteLine("disconnect handler from platformview camera!");
            VirtualView.PermissionRequest -= MapRequestPermissionEventHandler;
            VirtualView.PhotoRequest -= OnPhotoRequest;

            platformView.Dispose();
        }

        private async void OnPhotoRequest(
            object s, TaskCompletionSource<MemoryStream> tcs)
        {
            try
            {
                var _uiCameraView = PlatformView;
                var nsdata = await _uiCameraView.CapturePhoto();
                var ms = new MemoryStream();
                nsdata.AsStream().CopyTo(ms);
                tcs.SetResult(ms);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"coucou onPhotoRequest  e = {e}");
                tcs.SetException(e);
            }
        }




        //protected override void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        //{
        //    base.OnElementChanged(e);
        //    if (e.OldElement != null)
        //    {
        //        var elem = e.OldElement;
        //        elem.PhotoRequest -= OnPhotoRequest;
        //        elem.PermissionRequest -= OnPermissionRequest;
        //    }

        //    if (e.NewElement != null)
        //    {
        //        var elem = e.NewElement;
        //        RequestPermission(elem);
        //        elem.PhotoRequest -= OnPhotoRequest;
        //        elem.PhotoRequest += OnPhotoRequest;

        //        elem.PermissionRequest -= OnPermissionRequest;
        //        elem.PermissionRequest += OnPermissionRequest;
        //    }
        //}

        //private void DisplayLayer()
        //{
        //    System.Diagnostics.Debug.WriteLine("start DisplayLayer()");
        //    if (PlatformView == null)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"2 start DisplayLaye bounds={Bounds.ToString()}");

        //        _uiCameraView = new UICameraView(Bounds);
        //        SetNativeControl(_uiCameraView);
        //    }
        //}


        //protected override void Dispose(bool disposing)
        //{
        //    Debug.WriteLine("calling dispose en cameraviewrenderer");
        //    if (disposing)
        //    {
        //        _disposables.Clear();
        //        Control?.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        protected override UICameraView CreatePlatformView()
        {
            _uiCameraView = new UICameraView(VirtualView.Bounds);
            return _uiCameraView;
        }
    }
}
