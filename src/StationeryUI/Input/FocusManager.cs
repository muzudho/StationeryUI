namespace StationeryUI.Input;

/// <summary>Stable control IDs, ordered keyboard focus, modal scopes and pointer capture.</summary>
public sealed class FocusManager
{
    private readonly List<(string Id, string Scope, bool Enabled)> controls = [];
    private readonly Stack<(string Scope, string? Previous)> modals = [];
    public string? FocusedId { get; private set; }
    public string? CapturedId { get; private set; }
    public long? CapturedPointerId { get; private set; }
    public bool HasModal => modals.Count > 0;
    public void Register(string id, string scope = "root", bool enabled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (controls.Any(c => c.Id == id)) throw new ArgumentException("Duplicate control ID.", nameof(id));
        controls.Add((id, scope, enabled));
    }
    private bool Eligible((string Id, string Scope, bool Enabled) c) =>
        c.Enabled && c.Scope == (HasModal ? modals.Peek().Scope : "root");
    public void ClearFocus() => FocusedId = null;
    public bool Focus(string id)
    {
        if (!controls.Any(c => c.Id == id && Eligible(c))) return false;
        FocusedId = id;
        return true;
    }
    public void Move(bool reverse = false)
    {
        var candidates = controls.Where(Eligible).Select(c => c.Id).ToArray();
        if (candidates.Length == 0) { FocusedId = null; return; }
        var index = Array.IndexOf(candidates, FocusedId);
        FocusedId = candidates[index < 0 ? (reverse ? candidates.Length - 1 : 0)
            : (index + (reverse ? -1 : 1) + candidates.Length) % candidates.Length];
    }
    public void SetEnabled(string id, bool enabled)
    {
        var index = controls.FindIndex(c => c.Id == id);
        if (index < 0) throw new ArgumentException("Unknown control.", nameof(id));
        controls[index] = controls[index] with { Enabled = enabled };
        if (!enabled && FocusedId == id) { FocusedId = null; Move(); }
        if (!enabled && CapturedId == id) ReleasePointer();
    }
    public void PushModal(string scope)
    {
        modals.Push((scope, FocusedId));
        FocusedId = null;
        ReleasePointer();
        Move();
    }
    public void PopModal()
    {
        if (!modals.TryPop(out var modal)) return;
        ReleasePointer();
        FocusedId = null;
        if (modal.Previous is not { } id || !Focus(id)) Move();
    }
    public bool CapturePointer(string id, long pointerId)
    {
        if (CapturedId is not null || !controls.Any(c => c.Id == id && Eligible(c))) return false;
        CapturedId = id;
        CapturedPointerId = pointerId;
        return true;
    }
    public void ReleasePointer() { CapturedId = null; CapturedPointerId = null; }
    public void Deactivate() => ReleasePointer();
    public bool ConsumesKeyboard => FocusedId is not null || HasModal;
}
