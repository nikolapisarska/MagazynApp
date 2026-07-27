using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class DashboardPage
{
    public DashboardPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel; 
    }

    private void OnStartPickingClicked(object sender, EventArgs e)
    {
        _ = SafeNavigateAsync(nameof(MainPage));
    }

    private void OnSearchClicked(object sender, EventArgs e)
    {
        _ = SafeNavigateAsync(nameof(BoxSearchPage));
    }

    private static async Task SafeNavigateAsync(string route)
    {
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", $"Nie udało się przejść do strony: {ex.Message}", "OK");
        }
    }
}