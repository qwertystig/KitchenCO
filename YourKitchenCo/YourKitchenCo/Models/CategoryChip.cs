namespace YourKitchenCo.Models;

public class CategoryChip
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "🍽️";
    public string ImageUrl { get; set; } = string.Empty;

    public string DisplayLabel => $"{Icon} {TitleCase(Name)}";

    // Plain text, no emoji — for the icon-less category slider (reference
    // app's CategorySlider is text-only with an underline indicator).
    public string DisplayName => TitleCase(Name);

    private static string TitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        var words = value.ToLowerInvariant().Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i][1..];
        }
        return string.Join(' ', words);
    }
}
