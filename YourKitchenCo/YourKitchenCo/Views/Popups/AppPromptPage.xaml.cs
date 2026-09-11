using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Views.Popups;

public partial class AppPromptPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs = new();

    public Task<string?> ResultTask => _tcs.Task;

    public AppPromptPage(string title, string message, string initialValue = "", Keyboard? keyboard = null, string acceptText = "OK", string cancelText = "Cancel")
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        InputEntry.Text = initialValue;
        if (keyboard is not null)
            InputEntry.Keyboard = keyboard;

        var okButton = new Button
        {
            Text = acceptText,
            HeightRequest = 48,
            CornerRadius = 12,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15
        };
        okButton.SetAppThemeColor(BackgroundColorProperty,
            (Color)Application.Current!.Resources["Primary"],
            (Color)Application.Current!.Resources["PrimaryDark"]);
        okButton.SetAppThemeColor(Button.TextColorProperty,
            (Color)Application.Current!.Resources["White"],
            (Color)Application.Current!.Resources["PrimaryDarkText"]);
        okButton.Clicked += async (s, e) => await CloseAsync(InputEntry.Text);

        var cancelButton = new Button
        {
            Text = cancelText,
            HeightRequest = 48,
            CornerRadius = 12,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 1
        };
        cancelButton.SetAppThemeColor(Button.TextColorProperty,
            (Color)Application.Current!.Resources["Primary"],
            (Color)Application.Current!.Resources["PrimaryDark"]);
        cancelButton.SetAppThemeColor(Button.BorderColorProperty,
            (Color)Application.Current!.Resources["Primary"],
            (Color)Application.Current!.Resources["PrimaryDark"]);
        cancelButton.Clicked += async (s, e) => await CloseAsync(null);

        ButtonStack.Children.Add(okButton);
        ButtonStack.Children.Add(cancelButton);
    }

    private async Task CloseAsync(string? result)
    {
        await Task.WhenAll(
            CardBorder.FadeTo(0, 120, Easing.CubicIn),
            CardBorder.ScaleTo(0.92, 120, Easing.CubicIn));

        await Navigation.PopModalAsync(false);

        _tcs.TrySetResult(result);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.WhenAll(
            CardBorder.FadeTo(1, 180, Easing.CubicOut),
            CardBorder.ScaleTo(1, 180, Easing.SpringOut));

        InputEntry.Focus();
    }

    protected override bool OnBackButtonPressed()
    {
        if (!_tcs.Task.IsCompleted)
            _ = CloseAsync(null);

        return true;
    }
}
