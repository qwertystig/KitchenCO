using System.Globalization;

namespace YourKitchenCo.Converters;

/// <summary>
/// True when the bound value is a non-empty string, or any non-null object.
/// NOTE: CartPage.xaml already referenced {StaticResource IsNotNullOrEmptyConverter}
/// before this file existed anywhere in the project (and it wasn't registered in
/// App.xaml either) — that would throw a XamlParseException the first time
/// CartPage loaded. Added here and registered in App.xaml to fix that.
/// </summary>
public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s) return !string.IsNullOrWhiteSpace(s);
        return value is not null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
