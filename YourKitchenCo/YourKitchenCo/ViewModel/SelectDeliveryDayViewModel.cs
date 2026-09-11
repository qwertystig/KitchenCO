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
            // Reached mid-session from the cart prompt — just close this
            // page and return to whatever they were doing.
            await Shell.Current.Navigation.PopAsync();
        }
        else if (Application.Current is not null)
        {
            // First pick, right after login/registration — proceed into the app.
            Application.Current.MainPage = new AppShell();
        }
    }
}
