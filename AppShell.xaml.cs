using Microsoft.Maui.Controls;

namespace MagazynApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        
		// Rejestracja trasy dla MainPage
		Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
	}
}