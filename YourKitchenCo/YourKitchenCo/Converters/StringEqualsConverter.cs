using System.Globalization;

namespace YourKitchenCo.Converters;

/// <summary>True when the bound string equals ConverterParameter (case-insensitive). Used to show/hide a section based on a mode string like CurrentMenuType.</summary>
public class StringEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string;
        var compareTo = parameter as string;
        return text is not null && compareTo is not null && text.Equals(compareTo, StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
