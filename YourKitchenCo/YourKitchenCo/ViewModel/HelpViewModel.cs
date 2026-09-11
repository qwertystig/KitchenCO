using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Views;

namespace YourKitchenCo.ViewModel;

public partial class HelpViewModel : ObservableObject
{
    public string SupportEmail { get; } = "support@yourkitchenco.com";
    public string AppVersion { get; } = "1.0.0 (Debug)";

    [RelayCommand]
    private async Task ViewCancellationPolicyAsync() => await Shell.Current.GoToAsync(nameof(CancellationPolicyPage));
}
