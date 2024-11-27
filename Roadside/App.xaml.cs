namespace Roadside
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjH" +
                "TQxAR8/V1NDaF1cX2hIfEx0Q3xbf1x0ZF1MYVpbRH9PMyBoS35RckRiW3leeHVTR2BUWUN3");

            MainPage = new AppShell();
        }
    }
}
