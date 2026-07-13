using MagazynApp.ViewModels;
using System.Text.Json;
using CommunityToolkit.Maui.Storage;

namespace MagazynApp;

public partial class DashboardPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public DashboardPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        this.BindingContext = _viewModel;
    }

    private async void OnStartPickingClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MainPage));
    }

    private async void OnVerifyCartonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CartonVerificationPage());
    }

    async void OnExportButtonClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Wybierz typ eksportu:", "Anuluj", null, "Eksportuj produkty", "Eksportuj kartony");

        if (action == "Eksportuj produkty")
        {
            var data = await _viewModel.GetAllProductsFromService();
            await SaveJsonToFile("produkty.json", data);
        }
        else if (action == "Eksportuj kartony")
        {
            var data = await _viewModel.GetAllBoxesFromService();
            await SaveJsonToFile("kartony.json", data);
        }
    }

    private async Task SaveJsonToFile(string fileName, object data)
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
        using var stream = new MemoryStream(fileBytes);
        
        var result = await FileSaver.Default.SaveAsync(fileName, stream);
        if (result.IsSuccessful)
            await DisplayAlert("Sukces", "Zapisano plik: " + result.FilePath, "OK");
    }
}