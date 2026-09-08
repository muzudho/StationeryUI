namespace StationeryUI.MonoGame.Controls.ChartAxisSectionLabel;

using StationeryUI.MonoGame.Controls.SectionLabel;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

/// <summary>チャート軸の名前、表示ピン、目盛りラベルの配置を所有する文房具UIです。</summary>
public sealed class ChartAxisSectionLabelComponent
{
    public ChartAxisSectionLabelComponent(Rectangle bounds, string text, ChartAxisSide side)
    {
        Bounds = bounds;
        Text = text;
        Side = side;
    }

    public Rectangle Bounds { get; }
    public string Text { get; }
    public ChartAxisSide Side { get; }
    public SectionLabelComponent? SectionLabel { get; private set; }

    public void DrawHeader(StationeryDrawingTools draw, bool isPinned)
    {
        SectionLabel = SectionLabelComponent.CreateHorizontal(
            new Rectangle(Bounds.X, Bounds.Bottom + 8, Bounds.Width, 1),
            Text,
            new Color(5, 10, 18),
            new Color(170, 184, 188),
            labelHeight: Bounds.Height,
            labelGap: 8,
            surfaceOpacity: 0);
        SectionLabel.EnableVisibilityPin(isPinned);
        SectionLabel.Draw(draw);
    }

    public void DrawAxisLabels(
        StationeryDrawingTools draw,
        Rectangle plot,
        IReadOnlyList<string> labels,
        Color color,
        float scale)
    {
        if (labels.Count < 2) return;
        for (var index = 0; index < labels.Count; index++)
        {
            var text = labels[index];
            var centerY = plot.Top + plot.Height * index / (float)(labels.Count - 1);
            var textSize = draw.MeasureText(text);
            var rotatedHeight = textSize.X * scale;
            if (index == 0) centerY = plot.Top + rotatedHeight / 2f;
            if (index == labels.Count - 1) centerY = plot.Bottom - rotatedHeight / 2f;
            var centerX = Side == ChartAxisSide.Left ? plot.Left - 38 : plot.Right + 42;
            draw.DrawRotatedCenteredText(text, new Vector2(centerX + 1, centerY + 1), new Color(0, 0, 0, 135), scale);
            draw.DrawRotatedCenteredText(text, new Vector2(centerX, centerY), color, scale);
        }
    }

    public bool IsVisibilityPinHit(Point point) => SectionLabel?.IsVisibilityPinHit(point) ?? false;
}

public enum ChartAxisSide
{
    Left,
    Right,
}
