using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Microsoft.Maui.Platform;
using MinimalCameraApp.Controls;
using Size = Android.Util.Size;


namespace MinimalCameraApp.Platform.Renderers.Camera
{
    public class CameraDroid : FrameLayout, TextureView.ISurfaceTextureListener
    {
        private SparseIntArray ORIENTATIONS = new SparseIntArray();
        private CameraOptions _lastCameraOptionsUsed;
        private readonly Context _context;
        private int mSensorOrientation;
        public event EventHandler<MemoryStream> Photo;
        private readonly TextureView _cameraTexture;
        private SurfaceTexture _viewSurface;
        public bool OpeningCamera { private get; set; }
        private CameraManager _manager;
        public CameraDevice CameraDevice { get; set; }
        private Size _previewSize;
        private CaptureRequest.Builder _previewBuilder;
        private CameraCaptureSession _previewSession;
        private ImageReader _imReader;
        private readonly CameraStateListener _mStateListener;
        private HandlerThread _threadPreview;
        private bool _wasDetached = false;

        public CameraDroid(Context context) : base(context)
        {
            _context = context;
            _cameraTexture = new(context) { SurfaceTextureListener = this };
            _mStateListener = new() { Camera = this };

            AddView(_cameraTexture);

            MainActivity.Paused += CloseCamera;
            MainActivity.Resumed += ReopenCamera;
            // _cameraTexture.Click += (sender, args) => { TakePhoto(); };

            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 180);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 270);

            this.SetBackgroundResource(Resource.Drawable.camera_bg);
            this.SetClipToOutline(true);
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            System.Diagnostics.Debug.WriteLine("OnSurfaceTextureAvailable called!");
            _viewSurface = surface;
            ConfigureTransform(width, height);
            StartPreview();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            System.Diagnostics.Debug.WriteLine("OnSurfaceTextureAvailable destroyed!");

            _viewSurface = surface;
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            System.Diagnostics.Debug.WriteLine("OnSurfaceTextureAvailable size changed!");
            _viewSurface = surface;

        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            //System.Diagnostics.Debug.WriteLine("OnSurfaceTextureAvailable updated!");
            _viewSurface = surface;
        }

        public void OpenCamera(CameraOptions options)
        {
            _lastCameraOptionsUsed = options;
            if (_context == null || OpeningCamera)
            {
                System.Diagnostics.Debug.WriteLine("OpenCamera skip!");
                return;
            }
            System.Diagnostics.Debug.WriteLine("Start OpenCamera()");


            OpeningCamera = true;
            _manager = (CameraManager)_context.GetSystemService(Context.CameraService);
            var cameraId = _manager.GetCameraIdList()[(int)options];
            var characteristics = _manager.GetCameraCharacteristics(cameraId);
            var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            _previewSize = map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture)))[0];
            _manager.OpenCamera(cameraId, _mStateListener, null);
        }

        private void CloseCamera(object sender, EventArgs args)
        {
            _previewSession?.Close();
            _previewSession = null;
            if (CameraDevice != null)
            {
                try
                {
                    CameraDevice?.Close();

                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"cannot dispose camera ={ex.Message}");
                }

            }
            CameraDevice = null;

            StopBackgroundThread(_threadPreview);
            _threadPreview = null;
        }

        private void ReopenCamera(object sender, EventArgs args)
        {
            OpenCamera(_lastCameraOptionsUsed);
        }

        public void UnRegisterAndClose()
        {
            MainActivity.Paused -= CloseCamera;
            MainActivity.Resumed -= ReopenCamera;
            CloseCamera(null, null);
        }

        protected override void Dispose(bool disposing)
        {
            UnRegisterAndClose();
            base.Dispose(disposing);
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            if (_wasDetached)
            {
                _wasDetached = false;
                ReopenCamera(null, null);
            }
        }

        protected override void OnDetachedFromWindow()
        {
            UnRegisterAndClose();
            _wasDetached = true;
            base.OnDetachedFromWindow();
        }

        private void StopBackgroundThread(HandlerThread thread)
        {
            if (thread == null)
                return;

            thread.QuitSafely();
            try
            {
                thread.Join();
            }
            catch (InterruptedException e)
            {
                e.PrintStackTrace();
            }
        }

        ///////////////////////////////////

        public void TakePhoto()
        {
            if (_context == null || CameraDevice == null) {
                System.Diagnostics.Debug.WriteLine("Cannot take photo, no _context or CameraDevice found");
                return;
            }
            if (_previewSession == null) {
                System.Diagnostics.Debug.WriteLine("Cannot take photo, no _previewSession found");
                return;
            }
            _previewSession.StopRepeating();

            var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var captureBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
            captureBuilder.AddTarget(_imReader.Surface);
            captureBuilder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));

            int rotation = (int)windowManager.DefaultDisplay.Rotation;
            int orientation = GetOrientation(rotation);
            captureBuilder.Set(CaptureRequest.JpegOrientation, orientation);

            var captureRequest = captureBuilder.Build();
            var imageAvailableListener = new ImageAvailableListener();

            imageAvailableListener.Photo += (sender, buffer) =>
            {
                var stream = new MemoryStream(buffer);
                Photo?.Invoke(this, stream);
                UpdatePreview();
            };

            var backgroundHandler = new Handler(_threadPreview.Looper);
            _imReader.SetOnImageAvailableListener(imageAvailableListener, backgroundHandler);
            _previewSession.Capture(captureRequest, null, backgroundHandler);
        }

        public Android.Content.Res.Orientation GetDeviceDefaultOrientation()
        {
            var config = _context.Resources.Configuration;
            var wmanager = (IWindowManager)_context.GetSystemService(Context.WindowService);
            var rotation = wmanager.DefaultDisplay.Rotation;

            if (((rotation == SurfaceOrientation.Rotation0
                   || rotation == SurfaceOrientation.Rotation180)
                  && config.Orientation == Android.Content.Res.Orientation.Landscape)
                 || ((rotation == SurfaceOrientation.Rotation90
                      || rotation == SurfaceOrientation.Rotation270)
                     && config.Orientation == Android.Content.Res.Orientation.Portrait))
            {
                return Android.Content.Res.Orientation.Landscape;
            }
            else
            {
                return Android.Content.Res.Orientation.Portrait;
            }
        }

        private int GetOrientation(int rotation)
        {
            int orientation = mSensorOrientation - ORIENTATIONS.Get(rotation);
            orientation = orientation % 360;
            if (orientation < 0)
                orientation = 360 + orientation;
            return orientation;
        }

        public void StartPreview()
        {
            if (CameraDevice == null
                || !_cameraTexture.IsAvailable
                || _previewSize == null)
            {
                System.Diagnostics.Debug.WriteLine($"cannot start preview! textureAvailable={_cameraTexture.IsAvailable}");
                return;

            }
            System.Diagnostics.Debug.WriteLine($"start preview!");

            var texture = _cameraTexture.SurfaceTexture;
            texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);
            var surface = new Surface(texture);
            _previewBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
            _previewBuilder.AddTarget(surface);

            var captureStateListener = new CameraCaptureStateListener
            {
                OnConfigureFailedAction = session => {
                    System.Diagnostics.Debug.WriteLine("Failed to setup camera!!!");
                    _previewSession = null;
                },
                OnConfiguredAction = session =>
                {
                    System.Diagnostics.Debug.WriteLine($"initialize preview session={session}!");
                    _previewSession = session;
                    UpdatePreview();
                }
            };
            var characteristics = _manager.GetCameraCharacteristics(CameraDevice.Id);
            Size[] jpegSizes = null;
            if (characteristics != null)
            {
                var id = CameraCharacteristics.ScalerStreamConfigurationMap;
                var configMap = (StreamConfigurationMap)characteristics.Get(id);
                jpegSizes = configMap.GetOutputSizes((int)ImageFormatType.Jpeg);
            }

            int width = 0, height = 0;
            if (jpegSizes != null && jpegSizes.Length > 0)
            {
                // select smallest size that is > 256px
                System.Diagnostics.Debug.WriteLine($"jpegSizes.Length = {jpegSizes.Length}");
                for (var i = 0; i < jpegSizes.Length; ++i)
                {
                    int w = jpegSizes[i].Width;
                    int h = jpegSizes[i].Height;
                    if (w < 256 || h < 256)
                    {
                        continue;
                    }
                    if (width == 0 || height == 0)
                    {
                        width = w;
                        height = h;
                    }
                    else if (w < width || h < height)
                    {
                        width = w;
                        height = h;
                    }
                }
            }

            if (width == 0 || height == 0)
            {
                if (jpegSizes != null && jpegSizes.Length > 0)
                {
                    width = jpegSizes[0].Width;
                    height = jpegSizes[0].Height;
                }
                else
                {
                    width = 256;
                    height = 256;
                }
            }
            System.Diagnostics.Debug.WriteLine($"selected width = {width}");
            System.Diagnostics.Debug.WriteLine($"selected height = {height}");

            _imReader = ImageReader.NewInstance((int)width, (int)height, ImageFormatType.Jpeg, 1);
            mSensorOrientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);
            var surfaces = new List<Surface> { surface, _imReader.Surface };
            CameraDevice.CreateCaptureSession(surfaces, captureStateListener, null);
        }

        private void ConfigureTransform(int viewWidth, int viewHeight)
        {
            if (_viewSurface == null
                || _previewSize == null
                || _context == null)
            {
                System.Diagnostics.Debug.WriteLine("configure transform skipped");
                return;
            }
            System.Diagnostics.Debug.WriteLine("configure transform called");


            var windowManager = _context
                .GetSystemService(Context.WindowService)
                .JavaCast<IWindowManager>();
            var rotation = windowManager.DefaultDisplay.Rotation;
            var matrix = new Matrix();
            var viewRect = new Android.Graphics.RectF(0, 0, viewWidth, viewHeight);
            var bufferRect = new Android.Graphics.RectF(0, 0, _previewSize.Width, _previewSize.Height);
            var centerX = viewRect.CenterX();
            var centerY = viewRect.CenterY();

            if (rotation == SurfaceOrientation.Rotation90
                || rotation == SurfaceOrientation.Rotation270)
            {
                bufferRect.Offset(centerX - bufferRect.CenterX(),
                                  centerY - bufferRect.CenterY());
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
            }
            _cameraTexture.SetTransform(matrix);
        }

        private void UpdatePreview()
        {
            if (CameraDevice == null || _previewSession == null){
                System.Diagnostics.Debug.WriteLine("cannot update preview!");
                return;
            }
            System.Diagnostics.Debug.WriteLine("updating preview!");


            _previewBuilder.Set(CaptureRequest.ControlMode,
                                new Integer((int)ControlMode.Auto));
            if (_threadPreview == null)
            {
                _threadPreview = new HandlerThread("CameraPreview");
                _threadPreview.Start();
                //StopBackgroundThread(_threadPreview);
            }

            var backgroundHandler = new Handler(_threadPreview.Looper);
            var captureRequest = _previewBuilder.Build();
            _previewSession.SetRepeatingRequest(captureRequest, null, backgroundHandler);
        }


    }
}
