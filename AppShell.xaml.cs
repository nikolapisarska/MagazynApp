using Microsoft.Maui.Controls;

namespace MagazynApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
    
		Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
		Routing.RegisterRoute(nameof(CartonVerificationPage), typeof(CartonVerificationPage)); // DODAJ TO
	}
}