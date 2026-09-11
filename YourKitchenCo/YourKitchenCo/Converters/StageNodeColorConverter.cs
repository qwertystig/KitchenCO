using System.Globalization;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Converters;

/// <summary>
/// Node color for one stage of the 4-stage delivery tracker. Bound value is
/// the order's current Stage (int, 1-4); ConverterParameter is the stage
/// number this particular node represents ("1" through "4"). Also handles
/// the "Text" role for the node's number/checkmark label color.
/// </summary>
public class StageNodeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var currentStage = value is int i ? i : 1;

        // Parameter can be just the stage number ("2") for background, or
        // "2:Text" for the node's number/checkmark text color.
        var paramStr = parameter as string ?? "1";
        var parts = paramStr.Split(':');
        var thisStage = int.TryParse(parts[0], out var n) ? n : 1;
        var wantsText = parts.Length > 1 && parts[1] == "Text";

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        if (currentStage > thisStage) // completed — green background, white text
            return wantsText ? Colors.White : (isDark ? Color.FromArgb("#34D399") : Color.FromArgb("#059669"));

        if (currentStage == thisStage) // active — accent background, inverse text
        {
            if (wantsText) return isDark ? Color.FromArgb("#121212") : Colors.White;
            return isDark ? Color.FromArgb("#F7F2E8") : Color.FromArgb("#121212");
        }

        // upcoming — light/dark neutral background, secondary text
        if (wantsText) return isDark ? Color.FromArgb("#A1A1AA") : Color.FromArgb("#4E4E4E");
        return isDark ? Color.FromArgb("#2E2E32") : Color.FromArgb("#E5DFD3");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
