namespace StationeryUI.Text;

using System.Globalization;

/// <summary>単一行の編集状態。インデックスは文字要素単位で移動します。</summary>
public sealed class UnderlineTextEditor
{
    private int anchor;
    private readonly Stack<(string Text, int Caret, int Anchor)> undo = new();
    private readonly Stack<(string Text, int Caret, int Anchor)> redo = new();
    public UnderlineTextEditor(string text, int maximumLength = 100)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        MaximumLength = maximumLength;
        Insert(text);
        undo.Clear();
        SelectAll();
    }
    public int MaximumLength { get; }
    public string Text { get; private set; } = "";
    public int Caret { get; private set; }
    public int SelectionStart => Math.Min(anchor, Caret);
    public int SelectionLength => Math.Abs(anchor - Caret);
    public string SelectedText => Text.Substring(SelectionStart, SelectionLength);
    public void SelectAll() { anchor = 0; Caret = Text.Length; }
    public void MoveTo(int position, bool extend = false)
    {
        Caret = StringInfo.ParseCombiningCharacters(Text).Append(Text.Length)
            .Last(index => index <= Math.Clamp(position, 0, Text.Length));
        if (!extend) anchor = Caret;
    }
    public void Move(int direction, bool extend)
    {
        if (!extend && SelectionLength > 0)
        {
            MoveTo(direction < 0 ? SelectionStart : SelectionStart + SelectionLength);
            return;
        }
        var boundaries = StringInfo.ParseCombiningCharacters(Text).Append(Text.Length).ToArray();
        MoveTo(direction < 0 ? boundaries.LastOrDefault(index => index < Caret)
            : boundaries.FirstOrDefault(index => index > Caret, Text.Length), extend);
    }
    public void Insert(string value)
        => Insert(value, allowNewLines: false);

    public void Insert(string value, bool allowNewLines)
    {
        ArgumentNullException.ThrowIfNull(value);
        value = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        value = string.Concat(value.Where(character => !char.IsControl(character) || (allowNewLines && character == '\n')));
        var available = MaximumLength - (Text.Length - SelectionLength);
        var end = StringInfo.ParseCombiningCharacters(value).Append(value.Length).Last(index => index <= Math.Max(0, available));
        value = value[..end];
        if (value.Length == 0) return;
        SaveUndo();
        var start = SelectionStart;
        Text = Text.Remove(start, SelectionLength).Insert(start, value);
        Caret = anchor = StringInfo.ParseCombiningCharacters(Text).Append(Text.Length).First(i => i >= start + value.Length);
    }
    public void Delete(bool backward)
    {
        var before = (Text, Caret, anchor);
        if (SelectionLength == 0)
        {
            anchor = Caret;
            Move(backward ? -1 : 1, true);
        }
        if (SelectionLength == 0) return;
        SaveUndo(before);
        var start = SelectionStart;
        Text = Text.Remove(start, SelectionLength);
        Caret = anchor = start;
    }
    private void SaveUndo((string Text, int Caret, int Anchor)? snapshot = null)
    {
        undo.Push(snapshot ?? (Text, Caret, anchor));
        if (undo.Count > 100)
        {
            var retained = undo.Take(100).Reverse().ToArray();
            undo.Clear();
            foreach (var retainedSnapshot in retained) undo.Push(retainedSnapshot);
        }
        redo.Clear();
    }
    public void Undo() => Restore(undo, redo);
    public void Redo() => Restore(redo, undo);
    private void Restore(Stack<(string Text, int Caret, int Anchor)> from, Stack<(string Text, int Caret, int Anchor)> to)
    {
        if (!from.TryPop(out var state)) return;
        to.Push((Text, Caret, anchor));
        (Text, Caret, anchor) = state;
    }
}
