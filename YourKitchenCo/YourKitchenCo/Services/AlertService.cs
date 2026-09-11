using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YourKitchenCo.Views.Popups;

namespace YourKitchenCo.Services;

/// <summary>
/// Shows branded, theme-matched popups instead of the native OS alert dialog
/// (which ignores the app's colors entirely on every platform).
///
/// Also exposed as a static `Instance` (set once in App.xaml.cs at startup) so
/// ViewModels that already reach for `Shell.Current`/`Application.Current` for
/// alerts can call `AlertService.Instance....` directly without every one of
/// them needing a constructor-injection change — this is a deliberate,
/// pragmatic exception to the DI-everywhere pattern used elsewhere in the
/// app, scoped specifically to this cross-cutting UI concern.
/// </summary>
public class AlertService : IAlertService
{
    public static IAlertService Instance { get; set; } = null!;

    public async Task ShowAsync(string title, string message, string buttonText = "OK")
    {
        var nav = Application.Current?.MainPage?.Navigation;
        if (nav is null) return;

        var page = new AppAlertPage(title, message, buttonText);
        await nav.PushModalAsync(page, false);
        await page.ResultTask;
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string acceptText, string cancelText)
    {
        var nav = Application.Current?.MainPage?.Navigation;
        if (nav is null) return false;

        var page = new AppAlertPage(title, message, acceptText, cancelText);
        await nav.PushModalAsync(page, false);
        var resultIndex = await page.ResultTask;
        return resultIndex == 0;
    }

    public async Task<string?> ShowPromptAsync(string title, string message, string initialValue = "", Keyboard? keyboard = null, string acceptText = "OK", string cancelText = "Cancel")
    {
        var nav = Application.Current?.MainPage?.Navigation;
        if (nav is null) return null;

        var page = new AppPromptPage(title, message, initialValue, keyboard, acceptText, cancelText);
        await nav.PushModalAsync(page, false);
        return await page.ResultTask;
    }

    public async Task<string?> ShowActionSheetAsync(string title, string cancelText, params string[] options)
    {
        var nav = Application.Current?.MainPage?.Navigation;
        if (nav is null) return null;

        var page = new AppActionSheetPage(title, cancelText, options);
        await nav.PushModalAsync(page, false);
        return await page.ResultTask;
    }
}
