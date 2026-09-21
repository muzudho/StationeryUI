using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.MonoGame;
using StationeryUI.StyleDesigner;
using StationeryUI.Styling;
using StationeryUI.Windows;
using System.Text.Json.Nodes;

internal sealed partial class DesignerGame
{
    private StationeryUiHost? livePreview;
    private StationeryUiHost.Element? livePreviewTitle;
    private StyleBlueprint.Preview? previewSnapshot;
    private string? previewKey;
    private bool previewMouseDown;
    private ScreenRectangle previewWindow;

    private void ResetLivePreview()
    {
        livePreview?.Dispose(); livePreview = null; previewKey = null; previewSnapshot = null;
    }

    private void BuildLivePreviewHeader()
    {
        livePreviewTitle = Text("livePreviewTitle", new(560, 8, 708, 36), "編集プレビュー");
    }

    private void UpdateLivePreview(string json, MouseState mouse)
    {
        var origin = ui.Viewport.ToWindow(new ScreenRectangle(560, 48, 1, 1));
        previewWindow = new(origin.X, origin.Y,
            Math.Max(1, GraphicsDevice.Viewport.Width - origin.X - 8),
            Math.Max(1, GraphicsDevice.Viewport.Height - InspectorHeight - origin.Y - 8));
        livePreviewTitle!.Bounds = new(560, 8, previewWindow.Width / BodyScale, 36);
        var width = Math.Max(1, (int)previewWindow.Width);
        var height = Math.Max(1, (int)previewWindow.Height);
        var key = json + $"|{width}|{height}|{blueprint.SelectedLayoutId}|{selectedRow}|{selectedColumn}|{theme.Background}";
        if (key != previewKey)
        {
            var snapshot = blueprint.CreatePreview(width, height);
            ResetLivePreview(); previewSnapshot = snapshot;
            livePreview = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { Theme = theme, UseStationeryButtons = true };
            livePreview.Viewport.Offset = new(previewWindow.X, previewWindow.Y);
            var index = 0;
            ScreenRectangle Clip(ScreenRectangle rect)
            {
                var x = Math.Clamp(rect.X, 0, width); var y = Math.Clamp(rect.Y, 0, height);
                return new(x, y, Math.Max(0, Math.Min(width, rect.X + rect.Width) - x), Math.Max(0, Math.Min(height, rect.Y + rect.Height) - y));
            }
            void Block(ScreenRectangle rect, string text, bool outline = false)
            {
                var element = livePreview.AddTextBlock(livePreview.Root.AddChild("preview" + index++, "textBlock"), Clip(rect), text);
                if (outline) element.Theme = theme with { Surface = theme.TreeTarget };
            }
            void Outline(ScreenRectangle rect)
            {
                Block(new(rect.X, rect.Y, rect.Width, 1), "", true);
                Block(new(rect.X, rect.Y + rect.Height - 1, rect.Width, 1), "", true);
                Block(new(rect.X, rect.Y, 1, rect.Height), "", true);
                Block(new(rect.X + rect.Width - 1, rect.Y, 1, rect.Height), "", true);
            }
            Block(new(0, 0, width, height), "");
            foreach (var cell in snapshot.Cells)
                Block(cell.Bounds, $"{cell.Row + 1},{cell.Column + 1}");
            var metadata = JsonNode.Parse(snapshot.Json)!;
            bool Visible(string path) => path == snapshot.ScopePath || path.StartsWith(snapshot.ScopePath + "/", StringComparison.Ordinal);
            void Visit(JsonNode model, string parent)
            {
                var id = (string)model["id"]!;
                var path = parent + "/" + id;
                var kind = (string)model["type"]!;
                var visible = Visible(path);
                if (kind == "dialog" && path != snapshot.ScopePath) return;
                if (visible && snapshot.Layout.Bounds.TryGetValue(path, out var bounds))
                {
                    var rect = Clip(bounds);
                    var title = model["label"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : id;
                    var previewId = "preview" + index++;
                    switch (kind)
                    {
                        case "button": livePreview.AddButton(previewId, rect, title, () => { }); break;
                        case "textBox": livePreview.AddTextBox(previewId, rect, title, title); break;
                        case "link": livePreview.AddLink(previewId, rect, title, () => { }); break;
                        case "tree":
                            var tree = new TreeView { SelectOnInteraction = false };
                            tree.AddNode("placeholder", title + "（項目は C# で設定）");
                            livePreview.AddTree(previewId, rect, title, tree); break;
                        case "splitPane":
                            var binding = snapshot.Settings.Bindings.FirstOrDefault(b => b.ModelPath == path && b.FirstModel is not null);
                            if (binding is not null)
                                livePreview.AddSplitPane(livePreview.Root.AddChild(previewId, "splitPane"), Clip(snapshot.Layout.ContentBounds[path]), title,
                                    snapshot.Settings.Layouts.Single(l => l.Id == binding.Layout).Split!);
                            break;
                        default:
                            if (kind == "textBlock" || model["children"] is not JsonArray { Count: > 0 } && kind is not ("viewport" or "page")) Block(rect, title);
                            break;
                    }
                }
                if (model["children"] is JsonArray children) foreach (var child in children) Visit(child!, path);
            }
            foreach (var root in metadata["models"]!.AsArray()) Visit(root!, "");
            if (blueprint.CanEditPanel)
            {
                var panelBinding = snapshot.Settings.Bindings.FirstOrDefault(b => b.Layout == blueprint.SelectedLayoutId);
                if (panelBinding is not null) Outline(snapshot.Layout.ContentBounds[panelBinding.ModelPath]);
            }
            foreach (var cell in snapshot.Cells)
            {
                Outline(cell.Bounds);
                if (cell.Row == selectedRow && cell.Column == selectedColumn && cell.Bounds.Width > 4 && cell.Bounds.Height > 4)
                    Outline(new(cell.Bounds.X + 2, cell.Bounds.Y + 2, cell.Bounds.Width - 4, cell.Bounds.Height - 4));
            }
            previewKey = key;
        }
        livePreview!.Viewport.Offset = new(previewWindow.X, previewWindow.Y);
        livePreviewTitle!.Label = $"編集プレビュー {width}×{height}px" + (previewSnapshot!.Standalone ? "（未割り当ての定義）" : "");
        var down = mouse.LeftButton == ButtonState.Pressed;
        if (down && !previewMouseDown && blueprint.CanEditGrid)
        {
            var x = mouse.X - previewWindow.X; var y = mouse.Y - previewWindow.Y;
            if (x >= 0 && y >= 0 && x < width && y < height)
                foreach (var cell in previewSnapshot.Cells)
                    if (x >= cell.Bounds.X && y >= cell.Bounds.Y && x < cell.Bounds.X + cell.Bounds.Width && y < cell.Bounds.Y + cell.Bounds.Height)
                    {
                        selectedRow = cell.Row; selectedColumn = cell.Column;
                        break;
                    }
        }
        previewMouseDown = down;
    }

    private void DrawLivePreview()
    {
        livePreview?.Draw();
        if (livePreview is null || previewSnapshot is null) return;
        ScreenRectangle Shift(ScreenRectangle r) => r with { X = r.X + previewWindow.X, Y = r.Y + previewWindow.Y };
        var layout = previewSnapshot.Layout;
        var shifted = new StationeryLayoutResult(layout.Bounds.ToDictionary(p => p.Key, p => Shift(p.Value)), layout.ContentBounds)
        { BorderBounds = layout.BorderBounds.ToDictionary(p => p.Key, p => Shift(p.Value)) };
        livePreview.DrawPanelBorders(shifted, path => path == previewSnapshot.ScopePath || path.StartsWith(previewSnapshot.ScopePath + "/", StringComparison.Ordinal) || previewSnapshot.ScopePath.StartsWith(path + "/", StringComparison.Ordinal), previewWindow);
    }
}
