using System.Globalization;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;

namespace YourKitchenCo.Converters;

/// <summary>
/// Selected-state color for the Main Menu / Cycling Menu toggle and the
/// "All" category chip. Previously used two different hues (blue for Main,
/// pale-blue/charcoal for Cycling) as a decorative day-theme touch — the
/// client flagged the app as too colorful overall, and since the toggle
/// already shows "Main Menu"/"Cycling Menu" as plain text, a color
/// distinction here was never carrying real information. Simplified to one
/// consistent brand accent for both (now medium blue, per a later request
/// to move the whole palette to blue).
///
/// Accepts either the dashboard's CurrentMenu string ("Main"/"Weekly") or a
/// Product's MenuType enum (Static/Cycle) for backward compatibility with
/// existing bindings, but no longer branches on it.
/// </summary>
public class MenuIdentityColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var role = parameter as string ?? "Background";
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        if (isDark)
        {
            return role switch
            {
                "Text" => Colors.White,
                _ => Color.FromArgb("#1C1C1E")
            };
        }

        return role switch
        {
            "Text" => Colors.White,
            _ => Color.FromArgb("#121212") // one consistent accent for Background and Accent roles alike
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
