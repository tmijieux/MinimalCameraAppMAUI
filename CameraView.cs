using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace MinimalCameraApp.Controls
{
    public enum CameraOptions
    {
        Rear,
        Front
    }

    public class CameraView : View
    {
        public static readonly BindableProperty CameraProperty =
            BindableProperty.Create(
                nameof(Camera),
                typeof(CameraOptions),
                typeof(CameraView),
                CameraOptions.Front
            );

        public CameraOptions Camera
        {
            get => (CameraOptions)GetValue(CameraProperty);
            set => SetValue(CameraProperty, value);
        }

        public static readonly BindableProperty HasPermissionProperty =
            BindableProperty.Create(
                nameof(HasPermission),
                typeof(bool),
                typeof(CameraView),
                false
            );

        public bool HasPermission
        {
            get => (bool)GetValue(HasPermissionProperty);
            set => SetValue(HasPermissionProperty, value);
        }

        public event EventHandler<TaskCompletionSource<MemoryStream>> PhotoRequest;
        public Task<MemoryStream> TakePhoto()
        {
            if (PhotoRequest == null)
            {
                return null;
            }
            var photoTCS = new TaskCompletionSource<MemoryStream>();
            PhotoRequest.Invoke(this, photoTCS);
            return photoTCS.Task;
        }

        public event EventHandler PermissionRequest;
        public void RequestPermission()
        {
            PermissionRequest?.Invoke(this, null);
        }
    }
}
