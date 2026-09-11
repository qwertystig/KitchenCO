using Microsoft.Maui.Graphics;

namespace YourKitchenCo.Graphics;

public class ChartDatum
{
    public string Label { get; set; } = string.Empty;
    public float Value { get; set; }
    public Color Color { get; set; } = Colors.Gray;
}
