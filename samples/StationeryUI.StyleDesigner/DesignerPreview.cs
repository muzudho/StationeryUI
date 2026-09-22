using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.Inspection;
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
    private bool previewWasActive;
    private ScreenRectangle previewWindow;
    private ScreenRectangle? previewDialogBounds;
    private IReadOnlyDictionary<string, ScreenRectangle> previewDialogChildren = new Dictionary<string, ScreenRectangle>();

    private void ResetLivePreview()
    {
        livePreview?.Dispose(); livePreview = null; previewKey = null; previewSnapshot = null; previewDialogBounds = null; previewDialogChildren = new Dictionary<string, ScreenRectangle>();
    }

    private void BuildLivePreviewHeader()
    {
        livePreviewTitle = Text("livePreviewTitle", new(560, 8, 708, 36), "編集プレビュー");
    }

    private void UpdateLivePreview(string json, MouseState mouse, bool active = false)
    {
        if (!HasLayoutTarget) { ResetLivePreview(); return; }
        var origin = ui.Viewport.ToWindow(new ScreenRectangle(560, 48, 1, 1));
        previewWindow = new(origin.X, origin.Y,
            Math.Max(1, GraphicsDevice.Viewport.Width - origin.X - 8),
            Math.Max(1, GraphicsDevice.Viewport.Height - InspectorHeight - origin.Y - 8));
        livePreviewTitle!.Bounds = new(560, 8, previewWindow.Width / BodyScale, 36);
        var width = Math.Max(1, (int)previewWindow.Width);
        var height = Math.Max(1, (int)previewWindow.Height);
        var key = json + $"|{width}|{height}|{blueprint.SelectedLayoutId}|{selectedRow}|{selectedColumn}|{theme.Background}|{SelectedPreviewModelPath()}";
        if (key != previewKey)
        {
            var snapshot = blueprint.CreatePreview(width, height, TargetLayoutId);
            ResetLivePreview(); previewSnapshot = snapshot;
            livePreview = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { Theme = theme, UseStationeryButtons = true };
            livePreview.Viewport.Offset = new(previewWindow.X, previewWindow.Y);
            var index = 0;
            var selectedModelPath = SelectedPreviewModelPath();
            var selectedNode = selectedModelPath is null ? null : snapshot.Settings.Models[0].CreateTree().Resolve(selectedModelPath);
            var dialogNode = selectedNode;
            while (dialogNode is not null && dialogNode.Kind != "dialog") dialogNode = dialogNode.Parent;
            var dialogPath = dialogNode?.Path;
            var dialogBounds = dialogPath is null ? (ScreenRectangle?)null
                : new(width * .15, height * .16, width * .7, height * .62);
            previewDialogBounds = dialogBounds;
            var dialogChildren = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
            if (dialogNode is not null && dialogBounds is { } dialogArea)
            {
                var children = dialogNode.Children.ToArray();
                var buttons = children.Where(child => child.Kind is "button" or "link").ToArray();
                var content = children.Where(child => !buttons.Contains(child)).ToArray();
                var contentHeight = Math.Max(40, dialogArea.Height - 104);
                for (var i = 0; i < content.Length; i++)
                    dialogChildren[content[i].Path] = new(dialogArea.X + 28, dialogArea.Y + 28 + i * contentHeight / Math.Max(1, content.Length),
                        Math.Max(1, dialogArea.Width - 56), Math.Max(32, contentHeight / Math.Max(1, content.Length) - 12));
                for (var i = 0; i < buttons.Length; i++)
                    dialogChildren[buttons[i].Path] = new(dialogArea.X + 28 + i * (dialogArea.Width - 56) / Math.Max(1, buttons.Length),
                        dialogArea.Y + dialogArea.Height - 76, Math.Max(1, (dialogArea.Width - 56) / Math.Max(1, buttons.Length) - 12), 48);
            }
            previewDialogChildren = dialogChildren;
            ScreenRectangle Clip(ScreenRectangle rect)
            {
                var x = Math.Clamp(rect.X, 0, width); var y = Math.Clamp(rect.Y, 0, height);
                return new(x, y, Math.Max(0, Math.Min(width, rect.X + rect.Width) - x), Math.Max(0, Math.Min(height, rect.Y + rect.Height) - y));
            }
            void Block(ScreenRectangle rect, string text, bool outline = false, ButtonColor? surface = null)
            {
                var element = livePreview.AddTextBlock(livePreview.Root.AddChild("preview" + index++, "textBlock"), Clip(rect), text);
                if (outline || surface is not null) element.Theme = theme with { Surface = surface ?? theme.TreeTarget };
            }
            void Outline(ScreenRectangle rect)
            {
                Block(new(rect.X, rect.Y, rect.Width, 1), "", true);
                Block(new(rect.X, rect.Y + rect.Height - 1, rect.Width, 1), "", true);
                Block(new(rect.X, rect.Y, 1, rect.Height), "", true);
                Block(new(rect.X + rect.Width - 1, rect.Y, 1, rect.Height), "", true);
            }
            Block(new(0, 0, width, height), "", surface: dialogPath is null ? null : new ButtonColor(0, 0, 0, 140));
            if (dialogPath is null)
                foreach (var cell in snapshot.Cells)
                    Block(cell.Bounds, $"{cell.Row + 1},{cell.Column + 1}");
            var metadata = JsonNode.Parse(snapshot.Json)!;
            bool Visible(string path)
                => dialogPath is not null
                    ? path == dialogPath || path.StartsWith(dialogPath + "/", StringComparison.Ordinal)
                    : path == snapshot.ScopePath || path.StartsWith(snapshot.ScopePath + "/", StringComparison.Ordinal);
            void Visit(JsonNode model, string parent)
            {
                var id = (string)model["id"]!;
                var path = parent + "/" + id;
                var kind = (string)model["type"]!;
                var visible = Visible(path);
                if (kind == "dialog" && dialogPath is null && path != snapshot.ScopePath) return;
                if (visible && snapshot.Layout.Bounds.TryGetValue(path, out var bounds))
                {
                    var rect = Clip(dialogPath == path && dialogBounds is { } dialog ? dialog :
                        dialogChildren.GetValueOrDefault(path, bounds));
                    var title = model["label"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : id;
                    var previewId = "preview" + index++;
                    switch (kind)
                    {
                        case "dialog": Block(rect, ""); break;
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
                                    snapshot.Settings.Layouts.Single(l => l.Path == binding.Layout).Split!);
                            break;
                        default:
                            if (kind == "textBlock" || model["children"] is not JsonArray { Count: > 0 } && kind is not ("viewport" or "page")) Block(rect, title);
                            break;
                    }
                }
                if (model["children"] is JsonArray children) foreach (var child in children) Visit(child!, path);
            }
            foreach (var root in metadata["models"]!.AsArray()) Visit(root!, "");
            if (dialogPath is null && blueprint.CanEditPanel)
            {
                foreach (var (layoutKey, area) in snapshot.Layout.LayoutContentBounds)
                    if (layoutKey.EndsWith(":" + blueprint.SelectedLayoutId, StringComparison.Ordinal)) Outline(area);
            }
            previewKey = key;
        }
        livePreview!.Viewport.Offset = new(previewWindow.X, previewWindow.Y);
        livePreviewTitle!.Label = $"{TargetLayoutId} — プレビュー {width}×{height}px" + (previewSnapshot!.Standalone ? "（未割り当ての定義）" : "");
        var down = mouse.LeftButton == ButtonState.Pressed;
        // Ignore background input and the click that brings this window back to the foreground.
        if (active && previewWasActive && down && !previewMouseDown && blueprint.CanEditGrid)
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
        previewWasActive = active;
    }

    private void DrawLivePreview()
    {
        livePreview?.Draw();
        if (livePreview is null || previewSnapshot is null) return;
        ScreenRectangle Shift(ScreenRectangle r) => r with { X = r.X + previewWindow.X, Y = r.Y + previewWindow.Y };
        var layout = previewSnapshot.Layout;
        var shifted = new StationeryLayoutResult(layout.Bounds.ToDictionary(p => p.Key, p => Shift(p.Value)), layout.ContentBounds)
        { BorderBounds = layout.BorderBounds.ToDictionary(p => p.Key, p => Shift(p.Value)) };
        if (previewDialogBounds is null)
            livePreview.DrawPanelBorders(shifted, path => path == previewSnapshot.ScopePath || path.StartsWith(previewSnapshot.ScopePath + "/", StringComparison.Ordinal) || previewSnapshot.ScopePath.StartsWith(path + "/", StringComparison.Ordinal), previewWindow);
        var selectedPath = SelectedPreviewModelPath();
        var dialogSelectionPath = snapshotDialogPath();
        var component = previewDialogBounds is { } dialog && selectedPath is not null && dialogSelectionPath is not null &&
            (selectedPath == dialogSelectionPath || selectedPath.StartsWith(dialogSelectionPath + "/", StringComparison.Ordinal))
            ? Shift(selectedPath == dialogSelectionPath ? dialog : previewDialogChildren.GetValueOrDefault(selectedPath, dialog))
            : selectedPath is not null && layout.Bounds.TryGetValue(selectedPath, out var selectedBounds) ? Shift(selectedBounds) : (ScreenRectangle?)null;
        var margin = previewDialogBounds is null && selectedPath is not null && layout.MarginBounds.TryGetValue(selectedPath, out var selectedMargin) ? Shift(selectedMargin) : (ScreenRectangle?)null;
        var partitions = ParentPreviewPartitions();
        ScreenPoint ShiftPoint(ScreenPoint point) => new(point.X + previewWindow.X, point.Y + previewWindow.Y);
        livePreview.DrawPreviewGuides(margin, component, partitions.Select(line => new StationeryInspectionLine(ShiftPoint(line.Start), ShiftPoint(line.End))).ToArray());

        string? snapshotDialogPath()
        {
            var selected = selectedPath is null ? null : previewSnapshot.Settings.Models[0].CreateTree().Resolve(selectedPath);
            for (var node = selected; node is not null; node = node.Parent)
                if (node.Kind == "dialog") return node.Path;
            return null;
        }
    }

    private string? SelectedPreviewModelPath()
    {
        var item = styleTree?.Tree?.TargetItem;
        if (item is null) return null;
        var path = designerTreeMode == DesignerTreeMode.Json ? null : semanticTree.PathFor(item);
        if (path is null) return null;
        var separator = path.IndexOf(':');
        return separator >= 0 ? path[..separator] : path;
    }

    private IReadOnlyList<StationeryInspectionLine> ParentPreviewPartitions()
    {
        if (previewSnapshot is null || TargetLayoutId is null) return [];
        var settings = previewSnapshot.Settings;
        var target = settings.Layouts.FirstOrDefault(layout => layout.Path == TargetLayoutId);
        if (target is null) return [];
        var parentPath = target.ParentPath;
        if (parentPath is null)
        {
            var owner = settings.Bindings.FirstOrDefault(binding => binding.Layout == target.Path)?.ModelPath;
            if (owner is not null)
                parentPath = settings.Bindings.FirstOrDefault(binding =>
                    binding.Children.Any(child => child.ModelPath == owner) ||
                    binding.DockChildren.Any(child => child.ModelPath == owner))?.Layout;
        }
        if (parentPath is null) return [];
        var parentBinding = settings.Bindings.FirstOrDefault(binding => binding.Layout == parentPath);
        if (parentBinding is null || !previewSnapshot.Layout.LayoutContentBounds.TryGetValue(parentBinding.ModelPath + ":" + parentPath, out var content)) return [];
        return DeveloperInspectionPartitions.Create(settings.Layouts.Single(layout => layout.Path == parentPath), content);
    }
}
