using StationeryUI.Inspection;
using StationeryUI.Styling;
using System.Text.Json;

/// <summary>Connects stable code-defined roles to models nodes. Layouts wrappers never change these roles.</summary>
internal sealed record DemoModelBinding(StationeryNode Root, StationeryNode Dialog,
    IReadOnlyDictionary<string, StationeryNode> Main, IReadOnlyDictionary<string, StationeryNode> DialogControls, string Signature)
{
    public static StationeryStyleSettings Fallback { get; } = StationeryStyleSettings.Parse("""
        {
          "models": [{"id":"demo","type":"viewport","children":[
            {"id":"nameField","type":"textBox"}, {"id":"memoField","type":"textBox"},
            {"id":"themeButton","type":"button"}, {"id":"scaleButton","type":"button"},
            {"id":"applyTitleButton","type":"button"}, {"id":"openDialogButton","type":"button"},
            {"id":"sampleTree","type":"tree"},
            {"id":"editDialog","type":"dialog","children":[
              {"id":"nameField","type":"textBox"}, {"id":"cancelButton","type":"button"}, {"id":"saveButton","type":"button"}
            ]}
          ]}],
          "layouts":[
            {"id":"demoViewport","type":"panel"},
            {"id":"demoPage","type":"floating-layout","row-definitions":["1rate","1rate","1rate","1rate","1rate"],"column-definitions":["1rate","1rate"]}
          ],
          "bindings":[
            {"layout":"demoViewport","model":"demo"},
            {"layout":"demoPage","parentModel":"demo","childrenModel":[
              {"model":"nameField","row":0,"column":0}, {"model":"memoField","row":1,"column":0},
              {"model":"themeButton","row":2,"column":0}, {"model":"scaleButton","row":2,"column":1},
              {"model":"applyTitleButton","row":3,"column":0}, {"model":"openDialogButton","row":4,"column":0},
              {"model":"sampleTree","row":0,"column":1}
            ]}
          ]
        }
        """);

    public static DemoModelBinding Create(StationeryStyleSettings settings)
    {
        var root = settings.Models[0].CreateTree();
        var all = Descendants(root).ToArray();
        var dialogs = all.Where(node => node.Id == "editDialog" && node.Kind == "dialog").ToArray();
        if (dialogs.Length != 1) throw new JsonException("Demo models requires one editDialog of type dialog.");
        var dialog = dialogs[0];
        var main = Bind(all.Where(node => !node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["nameField"] = "textBox", ["memoField"] = "textBox", ["themeButton"] = "button",
            ["scaleButton"] = "button", ["applyTitleButton"] = "button", ["openDialogButton"] = "button", ["sampleTree"] = "tree"
        });
        var dialogControls = Bind(all.Where(node => node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["nameField"] = "textBox", ["cancelButton"] = "button", ["saveButton"] = "button"
        });
        var placed = settings.Bindings.SelectMany(binding => binding.Children).Select(child => child.ModelPath).ToHashSet(StringComparer.Ordinal);
        foreach (var node in main.Values)
            if (!placed.Contains(node.Path)) throw new JsonException($"Demo control {node.Path} needs a floating-layout cell binding.");
        if (settings.Bindings.Any(binding => root.Resolve(binding.ModelPath)!.IsWithin(dialog)) ||
            placed.Any(path => root.Resolve(path)!.IsWithin(dialog)))
            throw new JsonException("The demo dialog currently uses its code-defined layout; bind the main controls only.");
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
                throw new JsonException($"Demo models requires exactly one {id} of type {kind} in its main/dialog scope.");
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
