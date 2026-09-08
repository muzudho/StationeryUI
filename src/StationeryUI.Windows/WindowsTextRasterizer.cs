namespace StationeryUI.Windows;

using StationeryUI.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// WinForms と System.Drawing を使って文字列を透明背景の PNG へ変換します。
/// </summary>
public sealed class WindowsTextRasterizer(string fontFamily = "Meiryo") : ITextRasterizer
{
    public string FontFamily { get; } = !string.IsNullOrWhiteSpace(fontFamily) ? fontFamily : throw new ArgumentException("Font family required.", nameof(fontFamily));
    public byte[] RasterizePng(string text, int pixelHeight, bool bold)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (pixelHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelHeight));

        using var font = CreateFont(pixelHeight, bold);
        using var measuringBitmap = new System.Drawing.Bitmap(1, 1);
        using var measuringGraphics = System.Drawing.Graphics.FromImage(measuringBitmap);
        measuringGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var measured = measuringGraphics.MeasureString(string.IsNullOrEmpty(text) ? " " : text,
            font, int.MaxValue, System.Drawing.StringFormat.GenericTypographic);
        using var bitmap = new System.Drawing.Bitmap(Math.Max(1, (int)MathF.Ceiling(measured.Width)),
            Math.Max(1, (int)MathF.Ceiling(font.GetHeight(measuringGraphics))), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            graphics.DrawString(text, font, brush, new System.Drawing.PointF(0, 0), System.Drawing.StringFormat.GenericTypographic);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    public float MeasureTextWidth(string text, int pixelHeight, bool bold)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (pixelHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelHeight));
        if (text.Length == 0)
            return 0;

        using var font = CreateFont(pixelHeight, bold);
        using var measurementBitmap = new System.Drawing.Bitmap(1, 1);
        using var graphics = System.Drawing.Graphics.FromImage(measurementBitmap);
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        return graphics.MeasureString(
            text,
            font,
            int.MaxValue,
            System.Drawing.StringFormat.GenericTypographic).Width;
    }

    public int GetWrappedPageCount(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing)
    {
        ValidateWrappedTextArguments(text, width, height, pixelHeight, extraLineSpacing);
        using var font = CreateFont(pixelHeight, bold: false);
        using var measurementBitmap = new System.Drawing.Bitmap(1, 1);
        using var measurementGraphics = System.Drawing.Graphics.FromImage(measurementBitmap);
        measurementGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var lines = WrapLines(text, width, font, measurementGraphics);
        var lineHeight = GetLineHeight(font, measurementGraphics, extraLineSpacing);
        var linesPerPage = Math.Max(1, height / lineHeight);
        return Math.Max(1, (lines.Count + linesPerPage - 1) / linesPerPage);
    }

    public byte[] RasterizeWrappedPagePng(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing,
        int requestedPage)
    {
        ValidateWrappedTextArguments(text, width, height, pixelHeight, extraLineSpacing);
        using var font = CreateFont(pixelHeight, bold: false);
        using var measurementBitmap = new System.Drawing.Bitmap(1, 1);
        using var measurementGraphics = System.Drawing.Graphics.FromImage(measurementBitmap);
        measurementGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var lines = WrapLines(text, width, font, measurementGraphics);
        var lineHeight = GetLineHeight(font, measurementGraphics, extraLineSpacing);
        var linesPerPage = Math.Max(1, height / lineHeight);
        var pageCount = Math.Max(1, (lines.Count + linesPerPage - 1) / linesPerPage);
        var page = Math.Clamp(requestedPage, 0, pageCount - 1);

        using var bitmap = new System.Drawing.Bitmap(
            width,
            height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            var firstLine = page * linesPerPage;
            var lastLine = Math.Min(lines.Count, firstLine + linesPerPage);
            for (var index = firstLine; index < lastLine; index++)
            {
                graphics.DrawString(
                    lines[index],
                    font,
                    brush,
                    new System.Drawing.PointF(0, (index - firstLine) * lineHeight),
                    System.Drawing.StringFormat.GenericTypographic);
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    public int MeasureLineHeight(int pixelHeight, int extraLineSpacing)
    {
        using var font = CreateFont(pixelHeight, bold: false);
        using var bitmap = new System.Drawing.Bitmap(1, 1);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        return GetLineHeight(font, graphics, extraLineSpacing);
    }

    public int MeasureBaselineOffset(int pixelHeight)
    {
        using var font = CreateFont(pixelHeight, bold: false);
        var style = System.Drawing.FontStyle.Regular;
        var family = font.FontFamily;
        return Math.Max(1, (int)MathF.Round(
            font.Size * family.GetCellAscent(style) / family.GetEmHeight(style)));
    }

    private System.Drawing.Font CreateFont(int pixelHeight, bool bold) =>
        new(
            FontFamily,
            pixelHeight,
            bold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular,
            System.Drawing.GraphicsUnit.Pixel);

    private static int GetLineHeight(
        System.Drawing.Font font,
        System.Drawing.Graphics graphics,
        int extraLineSpacing) =>
        Math.Max(1, (int)MathF.Ceiling(font.GetHeight(graphics) + extraLineSpacing));

    private static List<string> WrapLines(
        string text,
        int maximumWidth,
        System.Drawing.Font font,
        System.Drawing.Graphics graphics)
    {
        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        if (normalized.Length > 100_000)
            normalized = normalized[..99_997] + "...";

        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (var character in normalized)
        {
            if (character == '\n')
            {
                lines.Add(line.ToString());
                line.Clear();
                continue;
            }

            line.Append(character);
            if (graphics.MeasureString(
                    line.ToString(),
                    font,
                    int.MaxValue,
                    System.Drawing.StringFormat.GenericTypographic).Width <= maximumWidth)
            {
                continue;
            }

            line.Length--;
            lines.Add(line.ToString());
            line.Clear();
            line.Append(character);
        }

        if (line.Length > 0 || lines.Count == 0)
            lines.Add(line.ToString());
        return lines;
    }

    private static void ValidateWrappedTextArguments(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (pixelHeight <= 0) throw new ArgumentOutOfRangeException(nameof(pixelHeight));
        if (extraLineSpacing < 0) throw new ArgumentOutOfRangeException(nameof(extraLineSpacing));
    }
}
