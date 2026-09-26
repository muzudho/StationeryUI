namespace StationeryUI.Editor;

using StationeryUI.Canvas;
using StationeryUI.Inspection;
using StationeryUI.Styling;

/// <summary>The parent shown around a selected child in both editor previews.</summary>
internal sealed record EditorPreviewGuides(ScreenRectangle? ParentBounds,
    IReadOnlyList<StationeryInspectionLine> Partitions)
{
    public static EditorPreviewGuides ForSelection(StyleBlueprint.Preview preview, string? selection)
    {
        if (selection is null) return new(null, []);
        var separator = selection.IndexOf(':');
        var modelPath = separator < 0 ? selection : selection[..separator];
        var settings = preview.Settings;
        var arranged = preview.Layout;
        var layoutPath = separator < 0 ? null : selection[(separator + 1)..];
        if (layoutPath is not null)
        {
            var layout = settings.Layouts.FirstOrDefault(node => node.Path == layoutPath);
            if (layout?.ParentPath is { } parentPath)
            {
                var key = modelPath + ":" + parentPath;
                if (arranged.LayoutContentBounds.TryGetValue(key, out var content))
                    return new(arranged.LayoutBounds.TryGetValue(key, out var layoutBounds) ? layoutBounds : null,
                        DeveloperInspectionPartitions.Create(settings.Layouts.Single(node => node.Path == parentPath), content));
            }
        }
        var parentBinding = settings.Bindings.FirstOrDefault(binding =>
            binding.Children.Any(child => child.ModelPath == modelPath)
            || binding.DockChildren.Any(child => child.ModelPath == modelPath)
            || binding.FirstModel == modelPath || binding.SecondModel == modelPath
            || binding.InspectorModel == modelPath);
        if (parentBinding is null) return new(null, []);
        var parentBounds = arranged.Bounds.TryGetValue(parentBinding.ModelPath, out var bounds)
            ? bounds : (ScreenRectangle?)null;
        var lines = arranged.LayoutContentBounds.TryGetValue(parentBinding.ModelPath + ":" + parentBinding.Layout, out var area)
            ? DeveloperInspectionPartitions.Create(settings.Layouts.Single(node => node.Path == parentBinding.Layout), area)
            : [];
        return new(parentBounds, lines);
    }
}
