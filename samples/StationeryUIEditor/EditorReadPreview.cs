using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.Inspection;
using StationeryUI.MonoGame;
using StationeryUI.Styling;
using StationeryUI.Editor;
using StationeryUI.Windows;
using System.Text.Json.Nodes;

internal sealed partial class EditorGame
{
    private StyleBlueprint? readPreviewBlueprint;
    private StationeryUiHost? readPreview;
    private StyleBlueprint.Preview? readPreviewSnapshot;
    private ScreenRectangle readPreviewWindow;
    private string? readPreviewKey;
    private bool showReadPreview;
    private DateTime readPreviewWriteTime;
    private double readPreviewRefreshElapsed;

    private void SetReadPreviewDocument(StyleBlueprint? plan, string? path)
    {
        readPreviewBlueprint = plan;
        readPreviewWriteTime = path is not null && File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        readPreviewRefreshElapsed = 0;
        readPreviewKey = null;
        readPreviewSnapshot = null;
        readPreview?.Dispose(); readPreview = null;
        showReadPreview = plan is not null;
        if (readPreviewButton is not null)
            readPreviewButton.Label = showReadPreview ? "詳細を見る" : "プレビューを見る";
    }

    private bool ContainsReadPreview(MouseState mouse)
        => mouse.X >= readPreviewWindow.X && mouse.X < readPreviewWindow.X + readPreviewWindow.Width
            && mouse.Y >= readPreviewWindow.Y && mouse.Y < readPreviewWindow.Y + readPreviewWindow.Height;

    private void RefreshReadPreviewFile(GameTime time)
    {
        if (readFile is null || (readPreviewRefreshElapsed += time.ElapsedGameTime.TotalSeconds) < .5) return;
        readPreviewRefreshElapsed = 0;
        try
        {
            var modified = File.GetLastWriteTimeUtc(readFile);
            if (modified == readPreviewWriteTime) return;
            var plan = StyleBlueprint.Open(readFile);
            if (readPreviewBlueprint is null)
            {
                showReadPreview = true;
                readPreviewButton!.Label = "詳細を見る";
            }
            readPreviewBlueprint = plan;
            readPreviewWriteTime = modified;
            readPreviewKey = null;
            if (launch.LivePipe is null) RefreshFileInspection(plan);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException
            or ArgumentException or InvalidOperationException)
        {
            readPreviewHeading!.Label = "プレビューの再読込に失敗：" + ex.Message;
        }
    }

    private void UpdateReadPreview()
    {
        readPreviewHeading!.Bounds = new();
        if (!showReadPreview || readPreviewBlueprint is null) return;
        var details = readView!.DetailsBounds;
        const double headingHeight = 36;
        readPreviewHeading.Bounds = new(details.X, details.Y, details.Width, headingHeight);
        readPreviewWindow = new(details.X, details.Y + headingHeight, Math.Max(1, details.Width),
            Math.Max(1, details.Height - headingHeight));
        var width = Math.Max(1, (int)readPreviewWindow.Width);
        var height = Math.Max(1, (int)readPreviewWindow.Height);
        var selected = readView.Model.SelectedEntry;
        var identity = selected?.Path;
        var targetLayout = selected?.Kind == "layout" ? identity?[(identity.IndexOf(':') + 1)..]
            : selected?.LayoutNodes?.FirstOrDefault(node => node.ParentPath == identity)?.Path.Split(':', 2)[1]
                ?? selected?.LayoutParentPath?.Split(':', 2)[1];
        var key = $"{width}|{height}|{identity}|{targetLayout}|{theme}";
        if (key == readPreviewKey) return;
        StyleBlueprint.Preview snapshot;
        try { snapshot = readPreviewBlueprint.CreatePreview(width, height, targetLayout); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
        {
            readPreviewHeading.Label = "プレビューを作れません：" + ex.Message;
            return;
        }
        readPreviewHeading.Label = "レイアウトプレビュー — " + (targetLayout ?? "既定のページ");
        var next = new StationeryUiHost(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { Theme = theme, UseStationeryButtons = true };
        next.Viewport.Offset = new(readPreviewWindow.X, readPreviewWindow.Y);
        var index = 0;
        ScreenRectangle Clip(ScreenRectangle rect)
        {
            var x = Math.Clamp(rect.X, 0, width); var y = Math.Clamp(rect.Y, 0, height);
            return new(x, y, Math.Max(0, Math.Min(width, rect.X + rect.Width) - x),
                Math.Max(0, Math.Min(height, rect.Y + rect.Height) - y));
        }
        void Block(ScreenRectangle rect, string label, ButtonColor? surface = null)
        {
            var element = next.AddTextBlock(next.Root.AddChild("readPreview" + index++, "textBlock"), Clip(rect), label);
            if (surface is not null) element.Theme = theme with { Surface = surface.Value };
        }
        Block(new(0, 0, width, height), "");
        foreach (var cell in snapshot.Cells) Block(cell.Bounds, $"{cell.Row + 1},{cell.Column + 1}");
        var metadata = JsonNode.Parse(snapshot.Json)!;
        void Visit(JsonNode model, string parent)
        {
            var id = (string)model["id"]!;
            var path = parent + "/" + id;
            var kind = (string)model["type"]!;
            if (kind == "dialog" && path != snapshot.ScopePath) return;
            if ((path == snapshot.ScopePath || path.StartsWith(snapshot.ScopePath + "/", StringComparison.Ordinal))
                && snapshot.Layout.Bounds.TryGetValue(path, out var bounds))
            {
                var rect = Clip(bounds);
                var label = model["label"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : id;
                var controlId = "readPreview" + index++;
                switch (kind)
                {
                    case "button": next.AddButton(controlId, rect, label, () => { }); break;
                    case "textBox": next.AddTextBox(controlId, rect, label, label); break;
                    case "link": next.AddLink(controlId, rect, label, () => { }); break;
                    case "tree":
                        var tree = new TreeView { SelectOnInteraction = false };
                        tree.AddNode("placeholder", label + "（項目はアプリで設定）");
                        next.AddTree(controlId, rect, label, tree); break;
                    case "splitPane":
                        var binding = snapshot.Settings.Bindings.FirstOrDefault(b => b.ModelPath == path && b.FirstModel is not null);
                        if (binding is not null)
                            next.AddSplitPane(next.Root.AddChild(controlId, "splitPane"),
                                Clip(snapshot.Layout.ContentBounds[path]), label,
                                snapshot.Settings.Layouts.Single(layout => layout.Path == binding.Layout).Split!);
                        break;
                    default:
                        if (kind == "textBlock" || model["children"] is not JsonArray { Count: > 0 }
                            && kind is not ("viewport" or "page")) Block(rect, label);
                        break;
                }
            }
            if (model["children"] is JsonArray children)
                foreach (var child in children) Visit(child!, path);
        }
        Visit(metadata["modelTree"]!, "");
        readPreview?.Dispose();
        readPreview = next;
        readPreviewSnapshot = snapshot;
        readPreviewKey = key;
    }

    private void DrawReadPreview()
    {
        if (readPreview is null || readPreviewSnapshot is null) return;
        readPreview.Viewport.Offset = new(readPreviewWindow.X, readPreviewWindow.Y);
        readPreview.Draw();
        var snapshot = readPreviewSnapshot;
        ScreenRectangle Shift(ScreenRectangle rect) => rect with
        { X = rect.X + readPreviewWindow.X, Y = rect.Y + readPreviewWindow.Y };
        var layout = snapshot.Layout;
        var shifted = new StationeryLayoutResult(layout.Bounds.ToDictionary(pair => pair.Key, pair => Shift(pair.Value)), layout.ContentBounds)
        { BorderBounds = layout.BorderBounds.ToDictionary(pair => pair.Key, pair => Shift(pair.Value)) };
        readPreview.DrawPanelBorders(shifted,
            path => path == snapshot.ScopePath || path.StartsWith(snapshot.ScopePath + "/", StringComparison.Ordinal)
                || snapshot.ScopePath.StartsWith(path + "/", StringComparison.Ordinal), readPreviewWindow);
        var identity = readView!.Model.SelectedPath;
        var separator = identity?.IndexOf(':') ?? -1;
        var modelPath = separator < 0 ? identity : identity![..separator];
        ScreenRectangle? component = separator >= 0 && layout.LayoutBounds.TryGetValue(identity!, out var selectedLayout)
            ? selectedLayout : modelPath is not null && layout.Bounds.TryGetValue(modelPath, out var selectedModel) ? selectedModel : null;
        ScreenRectangle? margin = separator >= 0 && layout.LayoutMarginBounds.TryGetValue(identity!, out var layoutMargin)
            ? layoutMargin : modelPath is not null && layout.MarginBounds.TryGetValue(modelPath, out var modelMargin) ? modelMargin : null;
        var guides = EditorPreviewGuides.ForSelection(snapshot, identity);
        ScreenPoint ShiftPoint(ScreenPoint point) => new(point.X + readPreviewWindow.X, point.Y + readPreviewWindow.Y);
        readPreview.DrawPreviewGuides(margin is { } m ? Shift(m) : null, component is { } c ? Shift(c) : null,
            guides.Partitions.Select(line => new StationeryInspectionLine(ShiftPoint(line.Start), ShiftPoint(line.End))).ToArray(),
            guides.ParentBounds is { } parent ? Shift(parent) : null);
    }
}
