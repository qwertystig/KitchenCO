using System.Collections.ObjectModel;
using YourKitchenCo.Models;

namespace YourKitchenCo.ViewModel;

/// <summary>A company plus its locations, for the admin Companies & Locations screen.</summary>
public class CompanyRow
{
    public Company Company { get; set; } = new();
    public ObservableCollection<CompanyLocation> Locations { get; } = new();
}
