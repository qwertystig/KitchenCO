using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YourKitchenCo.Models;

public partial class CustomizationGroup : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public bool IsMultiSelect { get; set; }
    public List<Option> Options { get; set; } = new();

    // ProductDetailPage renders BOTH a radio-button list and a checkbox list
    // for every group and hides whichever doesn't apply. Hidden or not, the
    // RadioButtons still exist and still enforce their GroupName's
    // one-at-a-time rule — and because they're two-way bound to the same
    // IsSelected flags as the checkboxes, ticking a second checkbox in a
    // multi-select group silently un-ticked the first (the "can only check
    // one" report). Each list now binds to one of these instead, so the
    // list that doesn't apply has no items at all — no hidden radios.
    private static readonly List<Option> None = new();
    public List<Option> SingleSelectOptions => IsMultiSelect ? None : Options;
    public List<Option> MultiSelectOptions => IsMultiSelect ? Options : None;
}
