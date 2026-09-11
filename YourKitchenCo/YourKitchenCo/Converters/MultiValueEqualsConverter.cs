using System.Globalization;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Converters;

/// <summary>
/// Compares two bound values for equality (case-insensitive if both are
/// strings) — used where a DataTrigger can't help because both sides are
/// bindings, not a binding vs. a literal. E.g. a category chip inside a
/// BindableLayout comparing its own Name against the ancestor ViewModel's
/// ActiveCategory to know whether it's the selected one.
/// </summary>
public class MultiValueEqualsConverter : IMultiValueConverter
{
    public object Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEqual = false;

        if (values is { Length: 2 })
        {
            isEqual = values[0] is string a && values[1] is string b
                ? a.Equals(b, StringComparison.OrdinalIgnoreCase)
                : Equals(values[0], values[1]);
        }

        if (targetType == typeof(FontAttributes))
            return isEqual ? FontAttributes.Bold : FontAttributes.None;

        return isEqual;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
