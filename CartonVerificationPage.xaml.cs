using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagazynApp;

public partial class CartonVerificationPage : ContentPage
{
    public CartonVerificationPage(VerificationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel; // To "spina" XAML z logiką
    }
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    
}