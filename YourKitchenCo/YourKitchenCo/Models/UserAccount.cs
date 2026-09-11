using System;

namespace YourKitchenCo.Models;

public class UserAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer"; // Customer, Kitchen Staff, Admin
    public bool IsActive { get; set; } = true;
    public DateTime JoinedDate { get; set; } = DateTime.Now;

    public string CompanyId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;

    // Which floor/pantry drop-off point within the assigned location — e.g.
    // "Floor 3" or "3rd Floor Pantry". Free text rather than a fixed list,
    // since floor naming conventions vary a lot between office buildings.
    public string DeliveryFloor { get; set; } = string.Empty;

    // Plain text is acceptable only because this is a mock/demo data store
    // with no real backend — a genuine implementation (once Supabase Auth
    // is wired in) must never store a raw password like this; it belongs
    // hashed, server-side, never in a client-readable model. Empty means no
    // password has been set (every seeded demo account, and the ad-hoc
    // guest fallback in LoginViewModel) — login accepts any password for
    // those, exactly as it already did before this field existed, so
    // nothing demo/testing relies on breaks. Only accounts that actually
    // went through registration (where a real password is captured) get a
    // real check.
    public string Password { get; set; } = string.Empty;
}
