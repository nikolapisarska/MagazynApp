using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagazynApp;

public partial class CartonVerificationPage : ContentPage
{
    public CartonVerificationPage()
    {
        InitializeComponent();
    }
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    
}