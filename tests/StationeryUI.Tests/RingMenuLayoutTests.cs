using StationeryUI.Canvas;
using StationeryUI.Controls;

internal static class RingMenuLayoutTests
{
    public static void Run()
    {
        foreach (var size in new[] { (1280d, 720d), (240d, 180d), (32d, 24d) })
        foreach (var anchor in new[] { new ScreenRectangle(0, 0, 44, 44), new ScreenRectangle(size.Item1 - 44, size.Item2 - 44, 44, 44) })
        foreach (var count in new[] { 1, 2, 3, 5, 9, 32 })
        {
            var layout = RingMenuLayout.Create(anchor, size.Item1, size.Item2, count);
            Equal(count, layout.Buttons.Count);
            for (var i = 0; i < count; i++)
            for (var j = i + 1; j < count; j++)
            {
                var a = layout.Buttons[i];
                var b = layout.Buttons[j];
                if (a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height)
                    throw new Exception("Ring buttons overlap.");
            }
            foreach (var bounds in layout.Buttons)
            {
                Equal(bounds.Width, bounds.Height);
                if (bounds.X < -0.001 || bounds.Y < -0.001 || bounds.X + bounds.Width > size.Item1 + 0.001 || bounds.Y + bounds.Height > size.Item2 + 0.001)
                    throw new Exception("Ring button escaped the viewport.");
                var dx = bounds.X + bounds.Width / 2 - layout.Center.X;
                var dy = bounds.Y + bounds.Height / 2 - layout.Center.Y;
                if (Math.Abs(Math.Sqrt(dx * dx + dy * dy) - layout.Radius) > 0.001)
                    throw new Exception("Ring button is not on the band centerline.");
            }
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}, got {actual}.");
    }
}
