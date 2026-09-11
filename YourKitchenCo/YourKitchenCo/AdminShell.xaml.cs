using Microsoft.Maui.Controls;

namespace YourKitchenCo;

public partial class AdminShell : Shell
{
    public AdminShell()
    {
        InitializeComponent();

        // Navigation and the live clock both moved to Views/AdminNavStrip,
        // embedded at the top of each of the 8 admin pages — nothing left
        // to wire up here now that the flyout drawer is gone.
    }
}
