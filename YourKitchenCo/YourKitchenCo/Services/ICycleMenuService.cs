using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

public interface ICycleMenuService
{
    /// <summary>Which week (1-8) of the 8-week cycle a given calendar date falls in.</summary>
    int GetCycleWeekNumber(DateOnly date);

    /// <summary>The rotating 5-category menu for a specific delivery date, or null if the day has no menu (weekend).</summary>
    Task<CycleMenuDay?> GetMenuForDateAsync(DateOnly date);

    /// <summary>The rotating menu for every date in a list — handy for building a week's worth of DeliveryDateOptions in one call.</summary>
    Task<Dictionary<DateOnly, CycleMenuDay?>> GetMenuForDatesAsync(IEnumerable<DateOnly> dates);

    /// <summary>The full 8-week structure ("Week 1".."Week 8"), for the admin cycle-menu screen.</summary>
    Task<Dictionary<string, List<CycleMenuDay>>> GetAllWeeksAsync();

    /// <summary>Which week number (1-8) is currently active, i.e. what GetCycleWeekNumber(today) resolves to.</summary>
    Task<int> GetActiveWeekNumberAsync();

    /// <summary>
    /// Admin override: makes the given week (1-8) the active one for the
    /// current calendar week, re-anchoring the whole 8-week rotation so it
    /// continues cycling normally from there afterwards.
    /// </summary>
    Task SetActiveWeekAsync(int weekNumber);

    /// <summary>
    /// Edits a single dish. categoryPropertyName must be one of "Main Meal",
    /// "Vegetarian Meal", "Healthy Meal", "Curry of the Day", "Gourmet Sandwich".
    /// </summary>
    Task UpdateCycleItemAsync(string weekKey, string dayName, string categoryPropertyName, string newValue);
}
