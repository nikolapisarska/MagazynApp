namespace MagazynApp;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private async void OnStartPickingClicked(object sender, EventArgs e)
    {

        await Shell.Current.GoToAsync(nameof(MainPage));
    }
    private async void OnVerifyCartonClicked(object sender, EventArgs e)
    {
        // Zakładając, że stworzysz stronę o nazwie CartonVerificationPage
        await Navigation.PushAsync(new CartonVerificationPage());
    }
}