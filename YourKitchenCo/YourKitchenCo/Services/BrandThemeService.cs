using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace YourKitchenCo.Services;

/// <summary>
/// Switches between the two app-wide brand colour themes: Mediterranean
/// Pantry (from the client's brand guideline deck) and Ink &amp; Cream (the
/// monochrome look, i.e. no override at all). Summer Harvest and Berry &amp;
/// Cream were retired at the client's request.
///
/// How it works: Colors.xaml defines every colour key the app's XAML binds
/// to via {StaticResource}/{AppThemeBinding}. Picking a theme here merges a
/// small override ResourceDictionary (Resources/Styles/Themes/*.xaml) on
/// top of Colors.xaml in Application.Resources.MergedDictionaries, which
/// only re-defines the handful of keys that actually carry brand colour
/// (page/card backgrounds, primary text, the Primary/BrandGold/Gold accent
/// keys, etc.) — anything it doesn't mention (grayscale, semantic
/// error/success colors, ...) simply falls through to Colors.xaml unchanged.
///
/// IMPORTANT LIMITATION: {StaticResource} and {AppThemeBinding} are resolved
/// once, when a page/control is constructed — not re-evaluated live. Calling
/// Apply() alone updates the dictionary, but any page already on screen keeps
/// showing its old, already-resolved colours. To actually repaint the app,
/// the caller must rebuild the visible Shell afterwards (see
/// SettingsViewModel.SelectBrandThemeAsync, which does this the same way
/// LoginViewModel/SelectDeliveryDayViewModel already rebuild it elsewhere in
/// this project: Application.Current.MainPage = new AppShell()/AdminShell()).
/// </summary>
public static class BrandThemeService
{
    /// <summary>
    /// The monochrome near-black-on-cream look — "Ink &amp; Cream" in the UI.
    /// The stored key stays "Current" so existing saved preferences keep working.
    /// </summary>
    public const string Current = "Current";
    public const string MediterraneanPantry = "MediterraneanPantry";

    private const string PrefKey = "settings.brand_theme";

    /// <summary>The last-applied theme, persisted across launches.</summary>
    public static string SavedTheme => Preferences.Default.Get(PrefKey, Current);

    /// <summary>
    /// Merges the given theme's colour overrides into Application.Resources
    /// (removing whichever one was merged before), and persists the choice
    /// unless <paramref name="persist"/> is false (used on cold start, where
    /// we're restoring the saved choice rather than making a new one).
    /// </summary>
    public static void Apply(string theme, bool persist = true)
    {
        if (persist)
            Preferences.Default.Set(PrefKey, theme);

        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;

        foreach (var stale in merged.OfType<IBrandThemeDictionary>().Cast<ResourceDictionary>().ToList())
            merged.Remove(stale);

        ResourceDictionary? overrideDict = theme switch
        {
            MediterraneanPantry => new Resources.Styles.Themes.MediterraneanPantryTheme(),
            _ => null, // "Current" (or anything unrecognised) => no override, base Colors.xaml stands
        };

        if (overrideDict is not null)
            merged.Add(overrideDict);
    }
}
