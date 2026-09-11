namespace YourKitchenCo.Views;

public partial class CancellationPolicyPage : ContentPage
{
    public CancellationPolicyPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAnimation.EntranceAsync(Content);
    }
}
