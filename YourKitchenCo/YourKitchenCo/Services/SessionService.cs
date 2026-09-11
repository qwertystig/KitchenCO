using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

public interface ISessionService
{
    UserAccount? CurrentUser { get; }
    bool IsLoggedIn { get; }
    bool HasSeenCutoffNotice { get; set; }

    /// <summary>
    /// The delivery day the person picked when they logged in (or last
    /// changed it from the cart prompt). Used as the default delivery date
    /// for static-menu items added to the basket — cycle-menu items still
    /// use their own fixed day since the dish itself only exists on one day.
    /// </summary>
    DateOnly? SelectedOrderingDate { get; set; }

    void SignIn(UserAccount user);
    void SignOut();
}

/// <summary>
/// Simple in-memory session holder, registered as a singleton. This is a
/// stand-in for Supabase Auth's session — when that's wired in, SignIn/SignOut
/// here would be driven by the auth state-change callback instead of being
/// called directly from LoginViewModel.
/// </summary>
public class SessionService : ISessionService
{
    public UserAccount? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser is not null;
    public bool HasSeenCutoffNotice { get; set; }
    public DateOnly? SelectedOrderingDate { get; set; }

    public void SignIn(UserAccount user)
    {
        CurrentUser = user;
        HasSeenCutoffNotice = false; // show the reminder again for a new login session
        SelectedOrderingDate = null; // force a fresh day pick each login session
    }

    public void SignOut()
    {
        CurrentUser = null;
        SelectedOrderingDate = null;
    }
}
