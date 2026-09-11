using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Views;

/// <summary>
/// One consistent "how things follow from one page to the next" entrance
/// animation, shared so every page looks and feels the same instead of some
/// pages snapping into view abruptly while others fade in.
/// </summary>
public static class PageAnimation
{
    public static async Task EntranceAsync(VisualElement? content)
    {
        if (content is null) return;

        content.Opacity = 0;
        content.TranslationY = 18;

        await Task.WhenAll(
            content.FadeTo(1, 260, Easing.CubicOut),
            content.TranslateTo(0, 0, 260, Easing.CubicOut));
    }
}
