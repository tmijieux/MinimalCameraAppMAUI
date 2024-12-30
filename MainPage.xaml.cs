namespace MinimalCameraApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void GoToCameraPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CameraPage());
        }
    }

}
