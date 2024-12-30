//using SkiaSharp;
//using Microsoft.Maui.Controls;
//using Microsoft.Maui;
//using SkiaSharp.Views.Maui.Controls;
//using SkiaSharp.Views.Maui;

//namespace MinimalCameraApp.Controls
//{
//    public class CameraFramingHelper : SKCanvasView
//    {
//        public CameraFramingHelper()
//        {
//            PaintSurface += OnPaintCanvas;
//        }

//        private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e)
//        {
//            var info = e.Info;
//            Draw(e.Surface.Canvas, info.Width, info.Height);
//        }

//        private void Draw(SKCanvas canvas, float width, float height)
//        {
//            canvas.Clear();
//            float xradius = width / 4.0f;
//            float yradius = height / 3.0f;
//            using (var paint = new SKPaint
//            {
//                Style = SKPaintStyle.Stroke,
//                Color = 0x88ffffff,
//                StrokeWidth = 10
//            })
//            {
//                canvas.DrawOval(width / 2, height / 2, xradius, yradius, paint);
//            }
//        }
//    }
//}
