
using MinimalCameraApp.Controls;

namespace MinimalCameraApp
{
    public class CameraPage : ContentPage
    {
        public CameraPage()
        {
            Content = new Grid
            {
                new Frame
                {
                    BorderColor = Colors.Red,
                    CornerRadius = 10,
                    Padding = 0,
                    IsClippedToBounds = true,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HeightRequest = 200,
                    WidthRequest = 200,
                    Content =  new CameraView {
                       HeightRequest = 200,
                       WidthRequest = 200,
                    }
                }
            };
        }

    }
}