using System.Text.Json;
using Microsoft.Maui.Storage;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

/// <summary>
/// Reads cycleMenu.json (bundled as a MauiAsset under Resources/Raw) and
/// resolves which week/day menu applies to a given calendar date. Also holds
/// the admin-facing controls: which week is "active" right now, and editing
/// individual dishes.
///
/// This is the "local file" version to get the feature working end to end.
/// When you move to Supabase, swap this implementation for one that reads
/// from a cycle_menu_weeks / cycle_menu_days table instead — the interface
/// (ICycleMenuService) doesn't need to change, so nothing that consumes it
/// (ViewModels, admin screens) will need to change either. The "active week"
/// override would become a row in a settings table instead of an in-memory
/// field, but SetActiveWeekAsync's re-anchoring math stays the same.
/// </summary>
public class CycleMenuService : ICycleMenuService
{
    /// <summary>
    /// The Monday that "Week 1" of the current 8-week cycle starts on.
    /// Mutable (not the original hardcoded constant) because SetActiveWeekAsync
    /// re-anchors it whenever an admin manually activates a specific week.
    /// </summary>
    private DateOnly _anchorMonday = new(2026, 8, 31);

    private const string AssetFileName = "cycleMenu.json";

    private Dictionary<string, List<CycleMenuDay>>? _weeks;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public int GetCycleWeekNumber(DateOnly date)
    {
        var mondayOfDate = GetMondayOfWeek(date);
        var mondayOfAnchor = GetMondayOfWeek(_anchorMonday);

        var weeksSinceAnchor = (mondayOfDate.DayNumber - mondayOfAnchor.DayNumber) / 7;

        // Modulo that stays positive even for dates before the anchor.
        var weekIndex = ((weeksSinceAnchor % 8) + 8) % 8;

        return weekIndex + 1; // 1..8, matching the "Week 1".."Week 8" keys in the JSON
    }

    public async Task<CycleMenuDay?> GetMenuForDateAsync(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return null;

        await EnsureLoadedAsync();

        var weekNumber = GetCycleWeekNumber(date);
        var weekKey = $"Week {weekNumber}";

        if (_weeks is null || !_weeks.TryGetValue(weekKey, out var days))
            return null;

        var match = days.FirstOrDefault(d => d.Day.Equals(date.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            match.WeekNumber = weekNumber;

        return match;
    }

    public async Task<Dictionary<DateOnly, CycleMenuDay?>> GetMenuForDatesAsync(IEnumerable<DateOnly> dates)
    {
        var result = new Dictionary<DateOnly, CycleMenuDay?>();
        foreach (var date in dates)
            result[date] = await GetMenuForDateAsync(date);

        return result;
    }

    public async Task<Dictionary<string, List<CycleMenuDay>>> GetAllWeeksAsync()
    {
        await EnsureLoadedAsync();
        return _weeks ?? new Dictionary<string, List<CycleMenuDay>>();
    }

    public Task<int> GetActiveWeekNumberAsync() =>
        Task.FromResult(GetCycleWeekNumber(DateOnly.FromDateTime(DateTime.Now)));

    public Task SetActiveWeekAsync(int weekNumber)
    {
        if (weekNumber < 1 || weekNumber > 8)
            throw new ArgumentOutOfRangeException(nameof(weekNumber), "Week number must be between 1 and 8.");

        // Re-anchor so that THIS calendar week resolves to the chosen week
        // number. Weeks before and after keep cycling normally from there —
        // e.g. activating Week 5 this week means next week is Week 6, not a
        // frozen repeat of Week 5.
        var thisMonday = GetMondayOfWeek(DateOnly.FromDateTime(DateTime.Now));
        _anchorMonday = thisMonday.AddDays(-7 * (weekNumber - 1));

        return Task.CompletedTask;
    }

    public async Task UpdateCycleItemAsync(string weekKey, string dayName, string categoryPropertyName, string newValue)
    {
        await EnsureLoadedAsync();

        if (_weeks is null || !_weeks.TryGetValue(weekKey, out var days))
            return;

        var day = days.FirstOrDefault(d => d.Day.Equals(dayName, StringComparison.OrdinalIgnoreCase));
        if (day is null) return;

        switch (categoryPropertyName)
        {
            case "Main Meal": day.MainMeal = newValue; break;
            case "Vegetarian Meal": day.VegetarianMeal = newValue; break;
            case "Healthy Meal": day.HealthyMeal = newValue; break;
            case "Curry of the Day": day.CurryOfTheDay = newValue; break;
            case "Gourmet Sandwich": day.GourmetSandwich = newValue; break;
        }
    }

    private static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private async Task EnsureLoadedAsync()
    {
        if (_weeks is not null) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_weeks is not null) return; // double-checked after acquiring the lock

            using var stream = await FileSystem.OpenAppPackageFileAsync(AssetFileName);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            _weeks = JsonSerializer.Deserialize<Dictionary<string, List<CycleMenuDay>>>(json)
                     ?? new Dictionary<string, List<CycleMenuDay>>();
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
