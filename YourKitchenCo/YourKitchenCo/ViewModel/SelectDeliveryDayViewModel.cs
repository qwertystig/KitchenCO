using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class SelectDeliveryDayViewModel : ObservableObject
{
    private readonly IOrderSchedulingService _schedulingService;
    private readonly ISessionService _session;

    [ObservableProperty]
    private DeliveryDateOption? _selectedDate;

    [ObservableProperty]
    private bool _isChangingDay; // true when reached mid-session (not the first post-login pick)

    public ObservableCollection<DeliveryDateOption> AvailableDates { get; } = new();

    public SelectDeliveryDayViewModel(IOrderSchedulingService schedulingService, ISessionService session)
    {
        _schedulingService = schedulingService;
        _session = session;
        IsChangingDay = Shell.Current is not null;
        LoadDates();
    }

    private void LoadDates()
    {
        AvailableDates.Clear();

        // Widest window (10 weekdays / 2 weeks) so this one screen covers
        // both the static menu's ordering window and the cycle menu's.
        var dates = _schedulingService.GetOrderableDeliveryDates(10);
        foreach (var date in dates)
            AvailableDates.Add(new DeliveryDateOption { Date = date });

        SelectedDate = AvailableDates.FirstOrDefault(d => d.Date == _session.SelectedOrderingDate)
                       ?? AvailableDates.FirstOrDefault();
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (SelectedDate is null) return;

        _session.SelectedOrderingDate = SelectedDate.Date;

        if (Shell.Current is not null)
        {
            // Reached mid-session as a popup over the current screen (see
            // SelectDeliveryDayPage.xaml) — close the popup and return to
            // whatever they were doing. Must be PopModalAsync to match the
            // PushModalAsync used to open it (they're separate nav stacks).
            await Shell.Current.Navigation.PopModalAsync();
        }
        else if (Application.Current is not null)
        {
            // First pick, right after login/registration — proceed into the app.
            Application.Current.MainPage = new AppShell();
        }
    }

    /// <summary>
    /// "Skip for now" — go into the app without actively choosing. The
    /// earliest orderable day (already preselected) becomes the default so
    /// the dashboard still has a date to show; they can change it from the
    /// date pill at any time.
    /// </summary>
    [RelayCommand]
    private async Task SkipAsync()
    {
        _session.SelectedOrderingDate ??= SelectedDate?.Date ?? AvailableDates.FirstOrDefault()?.Date;
        await EnterAppAsync(route: null);
    }

    /// <summary>"View my orders" — same as Skip, but lands on the Orders tab instead of the menu.</summary>
    [RelayCommand]
    private async Task ViewOrdersAsync()
    {
        _session.SelectedOrderingDate ??= SelectedDate?.Date ?? AvailableDates.FirstOrDefault()?.Date;
        await EnterAppAsync(route: "//activeorders");
    }

    private static async Task EnterAppAsync(string? route)
    {
        if (Shell.Current is not null)
        {
            // Mid-session: this is a modal over the app — just close it.
            await Shell.Current.Navigation.PopModalAsync();
        }
        else if (Application.Current is not null)
        {
            Application.Current.MainPage = new AppShell();
        }

        if (route is null) return;

        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch
        {
            // Non-fatal — they still land in the app, just on the default tab.
        }
    }
}
