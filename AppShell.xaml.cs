
namespace MagazynApp;

public partial class AppShell 
{
    public AppShell()
    {
        InitializeComponent();
    
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(BoxSearchPage), typeof(BoxSearchPage));
        Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage)); 
    }
}