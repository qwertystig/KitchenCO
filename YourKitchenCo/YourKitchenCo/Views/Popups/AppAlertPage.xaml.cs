using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Views.Popups;

public partial class AppAlertPage : ContentPage
{
    private readonly TaskCompletionSource<int> _tcs = new();

    /// <summary>Index of the button that was tapped (0 = first/primary button).</summary>
    public Task<int> ResultTask => _tcs.Task;

    public AppAlertPage(string title, string message, params string[] buttons)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;

        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            var isPrimary = i == 0;

            var button = new Button
            {
                Text = buttons[i],
                HeightRequest = 48,
                CornerRadius = 12,
                FontAttributes = FontAttributes.Bold,
                FontSize = 15
            };

            if (isPrimary)
            {
                button.SetAppThemeColor(BackgroundColorProperty,
                    (Color)Application.Current!.Resources["Primary"],
                    (Color)Application.Current!.Resources["PrimaryDark"]);
                button.SetAppThemeColor(Button.TextColorProperty,
                    (Color)Application.Current!.Resources["White"],
                    (Color)Application.Current!.Resources["PrimaryDarkText"]);
            }
            else
            {
                button.BackgroundColor = Colors.Transparent;
                button.BorderWidth = 1;
                button.SetAppThemeColor(Button.TextColorProperty,
                    (Color)Application.Current!.Resources["Primary"],
                    (Color)Application.Current!.Resources["PrimaryDark"]);
                button.SetAppThemeColor(Button.BorderColorProperty,
                    (Color)Application.Current!.Resources["Primary"],
                    (Color)Application.Current!.Resources["PrimaryDark"]);
            }

            button.Clicked += async (s, e) => await CloseWithResultAsync(index);
            ButtonStack.Children.Add(button);
        }
    }

    private async Task CloseWithResultAsync(int index)
    {
        await Task.WhenAll(
            CardBorder.FadeTo(0, 120, Easing.CubicIn),
            CardBorder.ScaleTo(0.92, 120, Easing.CubicIn));

        await Navigation.PopModalAsync(false);

        _tcs.TrySetResult(index);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.WhenAll(
            CardBorder.FadeTo(1, 180, Easing.CubicOut),
            CardBorder.ScaleTo(1, 180, Easing.SpringOut));
    }

    protected override bool OnBackButtonPressed()
    {
        // Hardware/gesture back acts like tapping the last (typically "Cancel") button.
        if (!_tcs.Task.IsCompleted)
            _ = CloseWithResultAsync(ButtonStack.Children.Count - 1);

        return true;
    }
}
