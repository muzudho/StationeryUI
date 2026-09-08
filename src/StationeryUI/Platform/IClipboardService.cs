namespace StationeryUI.Platform;

/// <summary>
/// OS のクリップボードへテキストを書き込みます。
/// </summary>
public interface IClipboardService
{
    bool TrySetText(string text);

    bool TryGetText(out string text);
}
