using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace YourKitchenCo.Graphics;

public class PieChartDrawable : IDrawable
{
    public List<ChartDatum> Slices { get; set; } = new();

    /// <summary>Set from the page to match the surrounding card background (light vs dark mode).</summary>
    public Color HoleColor { get; set; } = Colors.White;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var total = Slices.Sum(s => s.Value);

        if (Slices.Count == 0 || total <= 0)
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = 13;
            canvas.DrawString("No data for this range", dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        var diameter = Math.Min(dirtyRect.Width, dirtyRect.Height) - 16;
        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;
        var radius = diameter / 2;

        float startAngle = 0f;
        foreach (var slice in Slices)
        {
            var sweep = slice.Value / total * 360f;
            canvas.FillColor = slice.Color;
            canvas.FillArc(cx - radius, cy - radius, diameter, diameter, startAngle, startAngle + sweep, true);
            startAngle += sweep;
        }

        // Donut hole in the middle so it reads as a modern ring chart rather than a flat pie
        canvas.FillColor = HoleColor;
        var holeRadius = radius * 0.45f;
        canvas.FillEllipse(cx - holeRadius, cy - holeRadius, holeRadius * 2, holeRadius * 2);
    }
}
