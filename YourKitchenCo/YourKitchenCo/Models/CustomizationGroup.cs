using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
namespace YourKitchenCo.Models;

public partial class CustomizationGroup : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public bool IsMultiSelect { get; set; }
    public List<Option> Options { get; set; } = new();
}
