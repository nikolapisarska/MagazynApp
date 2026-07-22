using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MagazynApp;

public class BoolToTrueFalseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (bool?)value == true ? "WERYFIKACJA" : "STANDARD";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}