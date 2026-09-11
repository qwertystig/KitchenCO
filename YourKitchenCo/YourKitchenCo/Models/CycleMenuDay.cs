using System.Text.Json.Serialization;

namespace YourKitchenCo.Models;

/// <summary>One day's rotating menu — five categories, as they appear in cycleMenu.json.</summary>
public class CycleMenuDay
{
    [JsonPropertyName("DAY")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("MAIN MEAL")]
    public string MainMeal { get; set; } = string.Empty;

    [JsonPropertyName("VEGETARIAN MEAL")]
    public string VegetarianMeal { get; set; } = string.Empty;

    [JsonPropertyName("HEALTHY MEAL")]
    public string HealthyMeal { get; set; } = string.Empty;

    [JsonPropertyName("CURRY OF THE DAY")]
    public string CurryOfTheDay { get; set; } = string.Empty;

    [JsonPropertyName("GOURMET SANDWICH")]
    public string GourmetSandwich { get; set; } = string.Empty;

    /// <summary>Set by CycleMenuService when it resolves a day — not present in the JSON itself.</summary>
    [JsonIgnore]
    public int WeekNumber { get; set; }

    /// <summary>Convenience list for binding all five categories in a UI (e.g. a CollectionView).</summary>
    [JsonIgnore]
    public IEnumerable<(string Category, string Item)> Categories =>
        new[]
        {
            ("Main Meal", MainMeal),
            ("Vegetarian Meal", VegetarianMeal),
            ("Healthy Meal", HealthyMeal),
            ("Curry of the Day", CurryOfTheDay),
            ("Gourmet Sandwich", GourmetSandwich),
        };
}
