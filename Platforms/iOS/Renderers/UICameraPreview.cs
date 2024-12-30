using System.Diagnostics;
using AVFoundation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace MinimalCameraApp.Platform.Renderers.Camera
{
    public class UICameraView : UIView
    {
        public AVCaptureSession CaptureSession { get; private set; }
        private AVCaptureVideoPreviewLayer _previewLayer;
        private AVCaptureStillImageOutput _stillImageOutput;

        private CGRect _rect;

        public UICameraView(CGRect bounds)
        {
            try
            {
                CGRect rect = bounds;
                // Debug.WriteLine($"CTR bounds = (h,w) = ({bounds.Height},{bounds.Width})");
                rect.Height = rect.Width = (nfloat)Math.Max(rect.Height, rect.Width);
                // Debug.WriteLine($"CTR rect = (h,w) = ({rect.Height},{rect.Width})");

                _rect = rect;
                //Initialize(rect);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception on UICameraPreview Initialize(): {ex}");
            }
        }

        public override void Draw(CGRect rect)
        {
            if (_previewLayer == null)
            {
                _rect = rect;
                base.Draw(rect);
                return;
            }
            System.Diagnostics.Debug.WriteLine($"-- DRAW bounds = (h,w) = ({rect.Height},{rect.Width})");
            rect.Height = rect.Width = (nfloat)Math.Max(rect.Height, rect.Width);
            System.Diagnostics.Debug.WriteLine($"-- DRAW rect = (h,w) = ({rect.Height},{rect.Width})");
            _previewLayer.Frame = rect;
            base.Draw(rect);
        }

        public void Initialize()
        {
            Debug.WriteLine($"call UICameraPreview Initialize()");
            CGRect bounds = _rect;
            ClipsToBounds = true;
            CaptureSession = new AVCaptureSession();
            _previewLayer = new AVCaptureVideoPreviewLayer(CaptureSession)
            {
                Frame = bounds,
                //VideoGravity = AVLayerVideoGravity.ResizeAspect
                //
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill,
            };
            var session = AVCaptureDeviceDiscoverySession.Create(
                new[] { AVCaptureDeviceType.BuiltInWideAngleCamera },
                AVMediaTypes.Video,
                AVCaptureDevicePosition.Front
                );
            var videoDevices = session.Devices;
            session.Dispose();
            var device = videoDevices.FirstOrDefault();
            if (device == null)
            {
                Debug.WriteLine($"UICameraPreview Initialize() no devices");
                return;
            }

            var input = new AVCaptureDeviceInput(device, out NSError error);

            //var dictionary = new NSMutableDictionary();
            //dictionary[AVVideo.CodecKey] = new NSNumber((int)AVVideoCodec.JPEG);
            _stillImageOutput = new AVCaptureStillImageOutput();

            if (!CaptureSession.CanAddInput(input))
            {
                throw new InvalidOperationException("cannot add device input to capture session");
            }

            if (!CaptureSession.CanAddOutput(_stillImageOutput))
            {
                throw new InvalidOperationException("cannot add _stillImageOutput to capture session");
            }

            CaptureSession.AddInput(input);
            CaptureSession.AddOutput(_stillImageOutput);
            Layer.AddSublayer(_previewLayer);

            // Debug.WriteLine($"_previewLayer.Connection={_previewLayer.Connection}");
            SetupOrientation();

            Layer.MasksToBounds = true;
            Layer.CornerRadius = 15;

            Task.Run(() =>
            {
                try
                {
                    CaptureSession.StartRunning();
                    SetupOrientation();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception on UICameraPreview session StartRunning(): {ex}");
                }
            });
        }


        private UIDeviceOrientation? _previousOrientation = null;
        private void SetupOrientationConn(AVCaptureConnection conn)
        {
            var orientation = UIDevice.CurrentDevice.Orientation;
            if (orientation == UIDeviceOrientation.FaceUp
                || orientation == UIDeviceOrientation.FaceDown
                || orientation == UIDeviceOrientation.Unknown)
            {
                if (_previousOrientation != null)
                    orientation = _previousOrientation.Value;
                else
                    orientation = UIDeviceOrientation.LandscapeLeft;
            }
            conn.VideoOrientation = orientation switch
            {
                UIDeviceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeLeft,
                UIDeviceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeRight,
                UIDeviceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
                _ => AVCaptureVideoOrientation.Portrait,
            };
            _previousOrientation = orientation;
            // Debug.WriteLine($"orientation={UIDevice.CurrentDevice.Orientation}");
        }

        public void SetupOrientation()
        {
            if (_previewLayer == null || _previewLayer.Connection == null
                || Device.Idiom  == TargetIdiom.Phone)
            {
                return;
            }
            SetupOrientationConn(_previewLayer.Connection);
        }

        public override void LayoutSubviews()
        {
            // Debug.WriteLine($"LAYOUT _previewLayer.Connection={_previewLayer.Connection}");
            SetupOrientation();
            base.LayoutSubviews();
        }

        public async Task<NSData> CapturePhoto()
        {
            if (_stillImageOutput == null) {
                throw new InvalidOperationException("_stillImageOutput not initialized!");
            }

            try
            {
                var param = AVMediaTypes.Video.GetConstant();
                var conn = _stillImageOutput.ConnectionFromMediaType(param);
                SetupOrientationConn(conn);
                var buf = await _stillImageOutput.CaptureStillImageTaskAsync(conn);
                var jpeg = AVCaptureStillImageOutput.JpegStillToNSData(buf);
                return jpeg;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception on UICameraPreview CapturePhoto(): {ex}");
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Debug.WriteLine($"dispose native camera disposing={disposing}!");

            if (disposing)
            {
                Debug.WriteLine("disposing camera!");
                var s = CaptureSession;
                CaptureSession = null;
                if (s != null && s.Running)
                {
                    Debug.WriteLine("stop running session!");
                    s.StopRunning();
                }
                s?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
