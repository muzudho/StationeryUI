namespace StationeryUI.Text;

using StationeryUI.Canvas;

/// <summary>Text and composition offsets are UTF-16 indices, never native byte offsets.</summary>
public readonly record struct TextInputUpdate(string Text, bool IsComposition, int CaretIndex = 0, int SelectionLength = 0);

/// <summary>確定文字・IME の未確定文字・クリップボードへのプラットフォーム境界。</summary>
public interface ITextInputService : IDisposable
{
    void Start();
    void Stop();
    void SetInputArea(ScreenRectangle area);
    IReadOnlyList<TextInputUpdate> DrainUpdates();
    string ReadClipboard();
    void WriteClipboard(string text);
}
