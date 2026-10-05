using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

/// <summary>
/// Shared "which day am I ordering for" picker state, shown as an in-page
/// overlay (see Views/Controls/DeliveryDayOverlay.xaml) rather than a
/// separate modal page.
///
/// Why: this used to be SelectDeliveryDayPage, pushed on top of the current
/// screen via Shell.Current.Navigation.PushModalAsync, with the new page's
/// own background set to a translucent scrim so the screen underneath would
/// show through, dimmed. Field testing showed that doesn't actually work on
/// Android — the modal came up over a solid white background instead of the
/// dimmed menu. Pushing a new Page is not guaranteed to keep the previous
/// page composited underneath it; an overlay that lives inside the SAME
/// page's own visual tree (a Grid sibling, toggled by IsVisible) has no such
/// dependency, since the real page content is — genuinely, not
/// hypothetically — still right there underneath it.
///
/// Used by UserDashboardViewModel and ProductDetailViewModel, the two places
/// that offer "change delivery day" without leaving the current screen.
/// SelectDeliveryDayPage/SelectDeliveryDayViewModel are still used for the
/// one case that's genuinely a full page (navigation, not an overlay): the
/// mandatory first pick right after login/registration, before there's
/// anything "behind" it to preserve. Both now share the same full-screen
/// visual design (was a floating rounded card over a dimmed scrim — reported
/// as not looking clean, so both were redone to fill the whole screen
/// edge-to-edge instead of floating in the middle of it).
/// </summary>
public partial class DeliveryDayPickerState : ObservableObject
{
    private readonly IOrderSchedulingService _schedulingService;
    private readonly ISessionService _session;
    private readonly int _windowDays;
    private readonly Action? _onConfirmed;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private DeliveryDateOption? _selectedDate;

    public ObservableCollection<DeliveryDateOption> AvailableDates { get; } = new();

    public DeliveryDayPickerState(IOrderSchedulingService schedulingService, ISessionService session, int windowDays, Action? onConfirmed = null)
    {
        _schedulingService = schedulingService;
        _session = session;
        _windowDays = windowDays;
        _onConfirmed = onConfirmed;
    }

    /// <summary>Opens the overlay, loading the current orderable date window fresh each time.</summary>
    public void Open()
    {
        AvailableDates.Clear();
        foreach (var date in _schedulingService.GetOrderableDeliveryDates(_windowDays))
            AvailableDates.Add(new DeliveryDateOption { Date = date });

        SelectedDate = AvailableDates.FirstOrDefault(d => d.Date == _session.SelectedOrderingDate)
                       ?? AvailableDates.FirstOrDefault();

        IsVisible = true;
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedDate is not null)
        {
            _session.SelectedOrderingDate = SelectedDate.Date;
            _onConfirmed?.Invoke();
        }

        IsVisible = false;
    }

    [RelayCommand]
    private void Close() => IsVisible = false;
}
