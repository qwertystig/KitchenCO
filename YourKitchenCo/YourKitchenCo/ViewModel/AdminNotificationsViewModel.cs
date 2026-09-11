using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class AdminNotificationsViewModel : ObservableObject
{
    private readonly IUserDirectoryService _userDirectory;
    private readonly IOrderService _orderService;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _selectedAudience = "All Customers";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<string> AudienceOptions { get; } = new()
    {
        "All Users",
        "All Customers",
        "Kitchen Staff",
        "Active Order Holders"
    };

    public ObservableCollection<NotificationLog> SentHistory { get; } = new()
    {
        new NotificationLog { Title = "Weekend 20% Off Promo", Body = "Enjoy 20% off all pasta dishes using code WEEKEND20!", TargetAudience = "All Customers", SentAt = DateTime.Now.AddDays(-1), RecipientCount = 1420 },
        new NotificationLog { Title = "Kitchen Maintenance Notice", Body = "Scheduled app maintenance tonight from 2 AM to 3 AM.", TargetAudience = "All Users", SentAt = DateTime.Now.AddDays(-3), RecipientCount = 2850 }
    };

    public AdminNotificationsViewModel(IUserDirectoryService userDirectory, IOrderService orderService)
    {
        _userDirectory = userDirectory;
        _orderService = orderService;
    }

    private async Task<int> GetRecipientCountAsync(string audience)
    {
        var users = await _userDirectory.GetUsersAsync();

        switch (audience)
        {
            case "Kitchen Staff":
                return users.Count(u => u.Role == "Kitchen Staff");

            case "All Customers":
                return users.Count(u => u.Role == "Customer");

            case "Active Order Holders":
                var activeOrders = await _orderService.GetAllActiveOrdersAsync();
                return activeOrders.Select(o => o.UserId).Distinct().Count();

            default: // "All Users"
                return users.Count;
        }
    }

    [RelayCommand]
    private async Task SendNotificationAsync()
    {
        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
        {
            if (Application.Current?.MainPage != null)
            {
                await AlertService.Instance.ShowAsync("Missing Information", "Please provide both a title and message body.", "OK");
            }
            return;
        }

        IsBusy = true;

        // Simulate API broadcast delay
        await Task.Delay(1000);

        var recipientCount = await GetRecipientCountAsync(SelectedAudience);

        var newLog = new NotificationLog
        {
            Title = Title,
            Body = Message,
            TargetAudience = SelectedAudience,
            SentAt = DateTime.Now,
            RecipientCount = recipientCount
        };

        SentHistory.Insert(0, newLog);

        // Reset form
        Title = string.Empty;
        Message = string.Empty;
        IsBusy = false;

        if (Application.Current?.MainPage != null)
        {
            await AlertService.Instance.ShowAsync("Success", $"Notification broadcasted to {newLog.TargetAudience} ({newLog.RecipientCount} recipients).", "OK");
        }
    }
}