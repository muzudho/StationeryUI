using StationeryUI.Inspection;
using StationeryUI.Styling;
using System.Text.Json;

/// <summary>Connects stable code-defined roles to model nodes. Layout wrappers never change these roles.</summary>
internal sealed record DemoModelBinding(StationeryNode Root, StationeryNode Dialog,
    IReadOnlyDictionary<string, StationeryNode> Main, IReadOnlyDictionary<string, StationeryNode> DialogControls, string Signature)
{
    public static StationeryStyleSettings Fallback { get; } = StationeryStyleSettings.Parse("""
        {
          "model": {"id":"demo","type":"viewport","children":[
            {"id":"nameField","type":"textBox"}, {"id":"memoField","type":"textBox"},
            {"id":"themeButton","type":"button"}, {"id":"scaleButton","type":"button"},
            {"id":"applyTitleButton","type":"button"}, {"id":"openDialogButton","type":"button"},
            {"id":"editDialog","type":"dialog","children":[
              {"id":"nameField","type":"textBox"}, {"id":"cancelButton","type":"button"}, {"id":"saveButton","type":"button"}
            ]}
          ]},
          "layout":[{"id":"demo","type":"viewport"}]
        }
        """);

    public static DemoModelBinding Create(StationeryStyleSettings settings)
    {
        var root = settings.Model.CreateTree();
        var all = Descendants(root).ToArray();
        var dialogs = all.Where(node => node.Id == "editDialog" && node.Kind == "dialog").ToArray();
        if (dialogs.Length != 1) throw new JsonException("Demo model requires one editDialog of type dialog.");
        var dialog = dialogs[0];
        var main = Bind(all.Where(node => !node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["nameField"] = "textBox", ["memoField"] = "textBox", ["themeButton"] = "button",
            ["scaleButton"] = "button", ["applyTitleButton"] = "button", ["openDialogButton"] = "button"
        });
        var dialogControls = Bind(all.Where(node => node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["nameField"] = "textBox", ["cancelButton"] = "button", ["saveButton"] = "button"
        });
        var bound = main.Values.Concat(dialogControls.Values).ToHashSet();
        foreach (var node in all)
        {
            if (bound.Contains(node))
            {
                if (node.Children.Count != 0) throw new JsonException($"Control {node.Path} cannot own children in this demo.");
            }
            else if (node != root && node != dialog && node.Kind is not ("page" or "container"))
                throw new JsonException($"The demo has no code binding for {node.Path} ({node.Kind}).");
        }
        return new(root, dialog, main, dialogControls, string.Join('\n', all.Select(node => node.Path + ":" + node.Kind)));
    }

    private static IReadOnlyDictionary<string, StationeryNode> Bind(IEnumerable<StationeryNode> nodes, Dictionary<string, string> roles)
    {
        var all = nodes.ToArray();
        var result = new Dictionary<string, StationeryNode>(StringComparer.Ordinal);
        foreach (var (id, kind) in roles)
        {
            var matches = all.Where(node => node.Id == id).ToArray();
            if (matches.Length != 1 || matches[0].Kind != kind)
                throw new JsonException($"Demo model requires exactly one {id} of type {kind} in its main/dialog scope.");
            result.Add(id, matches[0]);
        }
        return result;
    }

    private static IEnumerable<StationeryNode> Descendants(StationeryNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in Descendants(child)) yield return descendant;
    }
}
