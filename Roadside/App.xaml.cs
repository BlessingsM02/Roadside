namespace Roadside
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzQyNzYzNEAzMjM2MmUzMDJl" +
                "MzBYSjU5dXJxdDJ0aGlZNXZZY3N4dHB2TTNzMDczZDJFQVZDYWV6cEpRZkVRPQ==");

            MainPage = new AppShell();
        }
    }
}
