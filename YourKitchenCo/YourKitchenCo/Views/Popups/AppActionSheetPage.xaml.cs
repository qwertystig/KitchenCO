using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Views.Popups;

public partial class AppActionSheetPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs = new();

    public Task<string?> ResultTask => _tcs.Task;

    public AppActionSheetPage(string title, string cancelText, params string[] options)
    {
        InitializeComponent();

        TitleLabel.Text = title;

        foreach (var option in options)
        {
            var button = new Button
            {
                Text = option,
                HeightRequest = 48,
                CornerRadius = 12,
                FontSize = 15,
                BackgroundColor = Colors.Transparent,
                BorderWidth = 1
            };
            button.SetAppThemeColor(Button.TextColorProperty,
                (Color)Application.Current!.Resources["PrimaryTextLight"],
                (Color)Application.Current!.Resources["PrimaryTextDark"]);
            button.SetAppThemeColor(Button.BorderColorProperty,
                (Color)Application.Current!.Resources["Primary"],
                (Color)Application.Current!.Resources["PrimaryDark"]);

            button.Clicked += async (s, e) => await CloseAsync(option);
            OptionStack.Children.Add(button);
        }

        CancelButton.Text = cancelText;
        CancelButton.SetAppThemeColor(Button.TextColorProperty,
            (Color)Application.Current!.Resources["Primary"],
            (Color)Application.Current!.Resources["PrimaryDark"]);
        CancelButton.SetAppThemeColor(Button.BorderColorProperty,
            (Color)Application.Current!.Resources["Primary"],
            (Color)Application.Current!.Resources["PrimaryDark"]);
        CancelButton.Clicked += async (s, e) => await CloseAsync(null);
    }

    private async Task CloseAsync(string? result)
    {
        await Task.WhenAll(
            CardBorder.FadeTo(0, 120, Easing.CubicIn),
            CardBorder.TranslateTo(0, 40, 120, Easing.CubicIn));

        await Navigation.PopModalAsync(false);

        _tcs.TrySetResult(result);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.WhenAll(
            CardBorder.FadeTo(1, 200, Easing.CubicOut),
            CardBorder.TranslateTo(0, 0, 200, Easing.CubicOut));
    }

    protected override bool OnBackButtonPressed()
    {
        if (!_tcs.Task.IsCompleted)
            _ = CloseAsync(null);

        return true;
    }
}
