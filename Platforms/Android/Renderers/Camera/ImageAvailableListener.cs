﻿using System;
using Android.Media;

namespace MinimalCameraApp.Platform.Renderers.Camera
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public event EventHandler<byte[]> Photo;
        public void OnImageAvailable(ImageReader reader)
        {
            Android.Media.Image image = null;
            try
            {
                image = reader.AcquireLatestImage();
                var buffer = image.GetPlanes()[0].Buffer;
                var imageData = new byte[buffer.Capacity()];
                buffer.Get(imageData);
                Photo?.Invoke(this, imageData);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                image?.Close();
            }
        }
    }
}
