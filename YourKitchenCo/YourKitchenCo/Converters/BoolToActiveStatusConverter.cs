using System.Globalization;

namespace YourKitchenCo.Converters;

public class BoolToActiveStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Active" : "Suspended";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
