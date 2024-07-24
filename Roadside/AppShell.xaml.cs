
using Roadside.Views;
namespace Roadside
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
        }

        private readonly static Type[] _routablePageTypes =
        [
            typeof(HistoryPage),
            typeof(ProfilePage),
            

        ];

        private static void RegisterRoutes()
        {
            foreach (var pageType in _routablePageTypes)
            {
                Routing.RegisterRoute(pageType.Name, pageType);
            }
        }
    }


}

