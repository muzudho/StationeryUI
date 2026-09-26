namespace StationeryUI.Editor;

internal enum EditorLaunchMode { Welcome, Read, Edit }

internal sealed record EditorLaunchOptions(EditorLaunchMode Mode, string? FilePath = null, string? LivePipe = null)
{
    public static EditorLaunchOptions Parse(string[] args)
    {
        if (args.Length == 0) return new(EditorLaunchMode.Welcome);
        // Keep the existing positional file argument editable during migration.
        if (args.Length == 1 && !args[0].StartsWith("--", StringComparison.Ordinal))
            return new(EditorLaunchMode.Edit, Path.GetFullPath(args[0]));
        string? file = null, pipe = null, mode = null;
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length) throw new ArgumentException($"{args[index]} requires a value.");
            var value = args[index + 1];
            switch (args[index])
            {
                case "--file" when file is null: file = Path.GetFullPath(value); break;
                case "--live" when pipe is null && !string.IsNullOrWhiteSpace(value): pipe = value; break;
                case "--mode" when mode is null && value is "read" or "edit": mode = value; break;
                default: throw new ArgumentException($"Unknown or duplicate editor option '{args[index]}'.");
            }
        }
        if (mode is null || mode == "edit" && file is null || file is null && pipe is null)
            throw new ArgumentException("Specify --mode read|edit and a --file or --live source.");
        return new(mode == "edit" ? EditorLaunchMode.Edit : EditorLaunchMode.Read, file, pipe);
    }
}
