namespace TrustInnova
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            if (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst() && !OperatingSystem.IsMacOS())
                this.Padding = new Thickness(0, 44, 0, 0);
            InitializeComponent();
        }
    }
}
