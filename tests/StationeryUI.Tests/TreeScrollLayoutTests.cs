using StationeryUI.Canvas;
using StationeryUI.Controls;

static class TreeScrollLayoutTests
{
    public static void Run()
    {
        var bounds = new ScreenRectangle(10, 20, 200, 100);
        var fits = TreeScrollLayout.Create(bounds, 200, 100, 17, 40, 50);
        Check(fits.Content == bounds && fits.MaximumX == 0 && fits.MaximumY == 0 && fits.OffsetX == 0 && fits.OffsetY == 0, "fitting content resets both offsets");
        var horizontal = TreeScrollLayout.Create(bounds, 300, 60, 17, 100, 0);
        Check(horizontal.Content.Width == 200 && horizontal.Content.Height == 83 && horizontal.VerticalTrack.Width == 0, "horizontal only");
        Check(horizontal.HorizontalThumb.X + horizontal.HorizontalThumb.Width == horizontal.HorizontalTrack.X + horizontal.HorizontalTrack.Width, "rightmost thumb reaches track end");
        var coupled = TreeScrollLayout.Create(bounds, 300, 100, 17, 999, 999);
        Check(coupled.Content == new ScreenRectangle(10, 20, 183, 83), "horizontal bar requires vertical bar");
        Check(coupled.OffsetX == 117 && coupled.OffsetY == 17, "offsets clamp to content viewport");
        Check(coupled.HorizontalTrack.Width == 183 && coupled.VerticalTrack.Height == 83, "tracks exclude shared corner");
        var reverse = TreeScrollLayout.Create(bounds, 200, 150, 17, 0, 0);
        Check(reverse.MaximumX == 17 && reverse.MaximumY == 67, "vertical bar requires horizontal bar");
        var resized = TreeScrollLayout.Create(bounds with { Width = 400, Height = 200 }, 300, 100, 17, coupled.OffsetX, coupled.OffsetY);
        Check(resized.MaximumX == 0 && resized.MaximumY == 0 && resized.OffsetX == 0 && resized.OffsetY == 0, "resizing removes bars and stale offsets");
        foreach (var scale in new[] { .5, 1, 1.5, 2.0 })
        {
            var zoom = TreeScrollLayout.Create(bounds, 400, 200, 17 / scale, 0, 0);
            Check(Math.Abs(zoom.HorizontalTrack.Height * scale - 17) < .001 && Math.Abs(zoom.VerticalTrack.Width * scale - 17) < .001, "17 screen-pixel bars at each scale");
        }
        foreach (var size in new[] { 0, 1, 10, 17 })
        {
            var tiny = TreeScrollLayout.Create(new(0, 0, size, size), 300, 400, 17, 1000, -1);
            foreach (var rectangle in new[] { tiny.Content, tiny.HorizontalTrack, tiny.HorizontalThumb, tiny.VerticalTrack, tiny.VerticalThumb })
                Check(double.IsFinite(rectangle.X) && double.IsFinite(rectangle.Y) && rectangle.Width >= 0 && rectangle.Height >= 0
                    && rectangle.X + rectangle.Width <= size && rectangle.Y + rectangle.Height <= size, "tiny viewport remains bounded");
        }
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
