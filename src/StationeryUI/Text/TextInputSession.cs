namespace StationeryUI.Text;

/// <summary>Owns editing focus and composition. The caller retains ownership of the OS service.</summary>
public sealed class TextInputSession(ITextInputService service, UnderlineTextEditor editor) : IDisposable
{
    public UnderlineTextEditor Editor { get; } = editor;
    public TextInputUpdate Composition { get; private set; } = new("", true);
    public bool IsFocused { get; private set; }
    public bool SuppressConfirmation { get; private set; }
    public void Focus()
    {
        if (IsFocused) return;
        service.Start();
        IsFocused = true;
        SuppressConfirmation = true;
    }
    public void Blur()
    {
        if (IsFocused) service.Stop();
        IsFocused = false;
        Composition = new("", true);
        SuppressConfirmation = true;
    }
    public void Update(bool enterDown, bool escapeDown)
    {
        if (!IsFocused) return;
        var hadComposition = Composition.Text.Length > 0;
        var updates = service.DrainUpdates();
        // SDL may split one commit into several UTF-8 events. Merge adjacent commits
        // in this pump so Undo restores the entire commit rather than one fragment.
        var committed = new System.Text.StringBuilder();
        void Flush()
        {
            if (committed.Length == 0) return;
            Editor.Insert(committed.ToString());
            committed.Clear();
        }
        foreach (var update in updates)
        {
            if (update.IsComposition) { Flush(); Composition = update; }
            else { committed.Append(update.Text); Composition = new("", true); }
        }
        Flush();
        if (hadComposition || updates.Count > 0 || Composition.Text.Length > 0) SuppressConfirmation = true;
        else if (!enterDown && !escapeDown) SuppressConfirmation = false;
    }
    public void Dispose() => Blur();
}
