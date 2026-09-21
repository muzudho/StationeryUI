using StationeryUI.Inspection;
using StationeryUI.Input;

internal static class StationeryNodeTests
{
    public static void Run()
    {
        var root = new StationeryNode("demo");
        var left = root.AddChild("leftPage", "page");
        var right = root.AddChild("rightPage", "page");
        var a = left.AddChild("nameField", "textBox");
        var b = right.AddChild("nameField", "textBox");
        Equal("/demo/leftPage/nameField", a.Path);
        Equal("/demo/rightPage/nameField", b.Path);
        Equal(a, root.Resolve(a.Path));
        Equal(b, a.Resolve(b.Path));
        Equal(root, b.Resolve("/demo"));
        Equal(null, root.Resolve("nameField"));
        Equal(null, root.Resolve("/demo/nameField"));
        Equal(null, root.Resolve("/demo/leftPage/NameField"));
        Equal(null, root.Resolve("/demo//leftPage/nameField"));
        Equal(null, root.Resolve(a.Path + "/"));
        Equal(null, root.Resolve("/other/leftPage/nameField"));
        Equal(true, a.IsWithin(root));
        Equal(false, a.IsWithin(right));
        Reject(() => left.AddChild("nameField"));
        foreach (var id in new[] { "", " ", "日本語", "a-b", "a.b", "a/b", "a\\b", "é", "a\nb", "a\0b" })
            Reject(() => new StationeryNode(id));
        foreach (var id in new[] { "camelCase", "A1", "_", "123", "name_2" }) Equal(id, new StationeryNode(id).Id);
        // Case is significant, while camelCase is the code-authoring convention.
        var capitalized = left.AddChild("NameField");
        Equal(capitalized, root.Resolve(capitalized.Path));
        var focus = new FocusManager();
        focus.Register(a.Path);
        focus.Register(b.Path);
        Equal(true, focus.Focus(a.Path));
        Equal(true, focus.CapturePointer(a.Path, 1));
        Equal(a.Path, focus.CapturedId);
        focus.Move();
        Equal(b.Path, focus.FocusedId);
        Equal(a.Path, focus.CapturedId);
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new Exception("Expected invalid or duplicate ID to be rejected.");
    }
    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
    }
}
