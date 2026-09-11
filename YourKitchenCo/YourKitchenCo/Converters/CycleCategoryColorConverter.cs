using System.Globalization;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Converters;

/// <summary>
/// Card/badge background + text for a category. Previously cycled every
/// category through 7 different saturated colors (bold red/orange/yellow/
/// sage/blue) — the client flagged the app as too colorful and unprofessional
/// as a direct result, so this was simplified to one clean, consistent
/// color for every category (now pale blue, per a later request to move the
/// whole palette to blue). Real food photography (ImageUrl) already does
/// the job of visually distinguishing one dish from another; the card
/// underneath it doesn't need to as well.
///
/// Kept as a converter (rather than deleting it and touching every binding
/// that references it) so this class of "too colorful" feedback can't
/// resurface from a missed spot — every card/badge/chip in the app reads
/// from this one place.
/// </summary>
public class CycleCategoryColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var wantsText = (parameter as string) == "Text";

        if (wantsText)
            return isDark ? Color.FromArgb("#F7F2E8") : Color.FromArgb("#121212");

        return isDark ? Color.FromArgb("#1C1C1E") : Colors.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
