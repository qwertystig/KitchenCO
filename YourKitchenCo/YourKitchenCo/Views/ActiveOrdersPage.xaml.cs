using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class ActiveOrdersPage : ContentPage
{
    /// <summary>
    /// Which of the two sections below is showing — "Active" or "History".
    /// Bound by the toggle buttons and both RefreshView triggers via
    /// {Binding SelectedTab, Source={x:Reference Root}}, the same
    /// BindableProperty + x:Reference pattern AdminNavStrip uses for its
    /// own active-route highlighting.
    /// </summary>
    public static readonly BindableProperty SelectedTabProperty =
        BindableProperty.Create(nameof(SelectedTab), typeof(string), typeof(ActiveOrdersPage), "Active");

    public string SelectedTab
    {
        get => (string)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public ActiveOrdersPage(ActiveOrdersViewModel viewModel, OrderHistoryViewModel historyViewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Order History keeps its own ViewModel as this section's
        // BindingContext — everything inside it (ItemsSource, the
        // RelativeSource command lookups in its DataTemplate) is the exact
        // markup OrderHistoryPage used to own, unchanged.
        HistorySection.BindingContext = historyViewModel;
    }

    private void OnActiveTabClicked(object? sender, EventArgs e) => SelectedTab = "Active";

    private void OnHistoryTabClicked(object? sender, EventArgs e) => SelectedTab = "History";

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Both lists only loaded once, in their ViewModel constructors — so an
        // order placed after this tab was first shown didn't appear until a
        // pull-to-refresh. Reload on every appearance instead.
        if (BindingContext is ActiveOrdersViewModel active)
            active.RefreshOrdersCommand.Execute(null);
        if (HistorySection.BindingContext is OrderHistoryViewModel history)
            history.RefreshHistoryCommand.Execute(null);

        // Shared entrance animation (PageAnimation.cs).
        await PageAnimation.EntranceAsync(Content);
    }
}
