using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace YourKitchenCo.Graphics;

public class BarChartDrawable : IDrawable
{
    public List<ChartDatum> Bars { get; set; } = new();
    public Color LabelColor { get; set; } = Colors.Gray;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Bars.Count == 0)
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = 13;
            canvas.DrawString("No data for this range", dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        var max = Bars.Max(b => b.Value);
        if (max <= 0) max = 1;

        const float labelAreaHeight = 26f;
        const float valueAreaHeight = 18f;
        var chartTop = 8f;
        var chartBottom = dirtyRect.Height - labelAreaHeight;
        var chartHeight = chartBottom - chartTop - valueAreaHeight;

        var slotWidth = dirtyRect.Width / Bars.Count;
        var barWidth = Math.Min(38f, slotWidth * 0.55f);

        for (var i = 0; i < Bars.Count; i++)
        {
            var bar = Bars[i];
            var barHeight = max > 0 ? bar.Value / max * chartHeight : 0;
            var slotCenter = slotWidth * i + slotWidth / 2;
            var x = slotCenter - barWidth / 2;
            var y = chartBottom - barHeight;

            canvas.FillColor = bar.Color;
            canvas.FillRoundedRectangle(x, y, barWidth, Math.Max(barHeight, 2), 6);

            canvas.FontColor = LabelColor;
            canvas.FontSize = 10;
            canvas.DrawString(bar.Value.ToString("0"),
                new RectF(slotCenter - slotWidth / 2, y - valueAreaHeight, slotWidth, valueAreaHeight),
                HorizontalAlignment.Center, VerticalAlignment.Bottom);

            canvas.DrawString(bar.Label,
                new RectF(slotCenter - slotWidth / 2, chartBottom + 4, slotWidth, labelAreaHeight),
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }

        canvas.StrokeColor = LabelColor.WithAlpha(0.3f);
        canvas.StrokeSize = 1;
        canvas.DrawLine(0, chartBottom, dirtyRect.Width, chartBottom);
    }
}
