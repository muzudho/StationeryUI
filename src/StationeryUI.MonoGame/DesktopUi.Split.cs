namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.Inspection;

public sealed partial class DesktopUi
{
    public Element AddLink(StationeryNode node, ScreenRectangle bounds, string label, Action clicked)
    {
        ValidateNode(node, "link");
        var element = new Element(node, bounds, label) { Click = clicked };
        Focus.Register(element.Path); elements.Add(element); return element;
    }
    public Element AddLink(string id, ScreenRectangle bounds, string label, Action clicked, StationeryNode? parent = null)
        => AddLink(AddNode(id, "link", parent), bounds, label, clicked);

    public Element AddSplitPane(StationeryNode node, ScreenRectangle bounds, string accessibleName, SplitPaneOptions options)
    {
        ValidateNode(node, "splitPane");
        var split = new SplitPane(); split.Configure(options);
        var element = new Element(node, bounds, accessibleName) { Split = split };
        Focus.Register(element.Path); elements.Add(element); return element;
    }
    public void BindSplitContent(Element split, Element first, Element second)
    {
        if (split.Split is null || first == second || !elements.Contains(split) || !elements.Contains(first) || !elements.Contains(second) ||
            first.Node.Parent != split.Node || second.Node.Parent != split.Node)
            throw new ArgumentException("Split content must be two direct child controls in the same UI.");
        split.FirstPane = first; split.SecondPane = second;
        ArrangeSplitPanes();
    }
    private ScreenRectangle FromWindow(ScreenRectangle bounds)
    {
        var origin = Viewport.ToLogical(new(bounds.X, bounds.Y));
        return new(origin.X, origin.Y, bounds.Width / Viewport.Scale, bounds.Height / Viewport.Scale);
    }
    private void ArrangeSplitPanes()
    {
        foreach (var element in elements.Where(e => e.Split is not null).OrderBy(e => e.Path.Count(c => c == '/')))
        {
            var arranged = element.Split!.Arrange(Viewport.ToWindow(element.Bounds));
            if (element.FirstPane is not null) element.FirstPane.Bounds = FromWindow(arranged.First);
            if (element.SecondPane is not null) element.SecondPane.Bounds = FromWindow(arranged.Second);
        }
    }
    private void UpdateSplit(Element element, ScreenPoint pointer, bool pressed, bool down, Func<Keys, bool> repeated)
    {
        var split = element.Split!;
        var window = Viewport.ToWindow(element.Bounds);
        var p = new ScreenPoint(pointer.X * Viewport.Scale + Viewport.Offset.X, pointer.Y * Viewport.Scale + Viewport.Offset.Y);
        var divider = split.Arrange(window).Divider;
        var coordinate = split.Options.Horizontal ? p.Y : p.X;
        if (Focus.CapturedId != element.Path) element.DraggingSplit = false;
        if (pressed && Focus.CapturedId == element.Path && Contains(divider, p))
        {
            element.DraggingSplit = true;
            element.SplitGrab = coordinate - (split.Options.Horizontal ? divider.Y : divider.X);
        }
        if (element.DraggingSplit) split.Drag(window, coordinate, element.SplitGrab);
        if (!down) element.DraggingSplit = false;
        if (Focus.FocusedId == element.Path && !element.DraggingSplit)
        {
            if (repeated(split.Options.Horizontal ? Keys.Up : Keys.Left)) split.SetRatio(split.Ratio - .05);
            if (repeated(split.Options.Horizontal ? Keys.Down : Keys.Right)) split.SetRatio(split.Ratio + .05);
        }
    }
}
