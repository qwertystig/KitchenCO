using System.Globalization;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Converters;

/// <summary>
/// Renders one star in a 5-star row: "★" if the bound Rating is at least
/// this star's position (via ConverterParameter, "1" through "5"), "☆"
/// otherwise. Used for both the customer's tappable rating row and the
/// admin's read-only display of it.
/// </summary>
public class RatingStarConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rating = value is int i ? i : 0;
        var position = parameter is string s && int.TryParse(s, out var p) ? p : 0;
        return rating >= position ? "★" : "☆";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
