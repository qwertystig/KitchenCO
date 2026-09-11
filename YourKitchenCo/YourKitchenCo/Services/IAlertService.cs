using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Services;

public interface IAlertService
{
    /// <summary>Single-button informational alert. Mirrors Page.DisplayAlert(title, message, cancel).</summary>
    Task ShowAsync(string title, string message, string buttonText = "OK");

    /// <summary>
    /// Two-button confirm dialog. Mirrors Page.DisplayAlert(title, message, accept, cancel).
    /// Returns true if the first (accept) button was tapped.
    /// </summary>
    Task<bool> ShowConfirmAsync(string title, string message, string acceptText, string cancelText);

    /// <summary>
    /// Text-entry dialog. Mirrors Page.DisplayPromptAsync. Returns the typed
    /// text, or null if cancelled (matches the native API's null-on-cancel behavior).
    /// </summary>
    Task<string?> ShowPromptAsync(string title, string message, string initialValue = "", Keyboard? keyboard = null, string acceptText = "OK", string cancelText = "Cancel");

    /// <summary>
    /// Bottom-sheet style option picker. Mirrors Page.DisplayActionSheet(title, cancel, destruction, buttons).
    /// Returns the tapped option's text, or null if cancelled.
    /// </summary>
    Task<string?> ShowActionSheetAsync(string title, string cancelText, params string[] options);
}
