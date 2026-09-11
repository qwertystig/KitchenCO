using System.Globalization;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Converters;

/// <summary>
/// True if the bound numeric value is greater than zero. Exists because
/// IsNotNullOrEmptyConverter has been mistakenly reached for on non-nullable
/// value types (decimal, int) several times across this app — a boxed
/// value type is never null, so that converter always returns true
/// regardless of the actual value, silently showing things that should be
/// hidden when the amount is zero. Use this instead for "hide if zero."
/// </summary>
public class GreaterThanZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            decimal d => d > 0,
            int i => i > 0,
            double db => db > 0,
            float f => f > 0,
            _ => false
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
