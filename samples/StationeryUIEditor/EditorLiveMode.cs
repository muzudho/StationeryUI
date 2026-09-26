using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Editor;
using StationeryUI.Inspection;
using StationeryUI.MonoGame;
using StationeryUI.Windows;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

internal sealed partial class EditorGame
{
    private EditorLiveConnection? liveConnection;
    private EditorLiveConnection.Packet? livePacket;
    private long lastShowSequence = -1, lastCaptureSequence;
    private bool liveDisconnectedReported;
    private string? lastStyleError;
    private string liveStyleStatus = "";

    private void StartLiveConnection()
    {
        if (launch.LivePipe is null) return;
        readView ??= new(GraphicsDevice, input, family => new WindowsTextRasterizer(family), StationeryDeveloperStyle.Load());
        readView.EmbeddedInEditor = true;
        readView.Theme = theme with { Selected = theme.Surface };
        liveConnection = new(launch.LivePipe);
        liveStyleStatus = "アプリへの接続を待っています…";
    }

    private void ReceiveLivePacket()
    {
        livePacket = liveConnection?.Take();
        if (livePacket is null) return;
        var message = livePacket.Message;
        liveStyleStatus = GetLiveStyleStatus(message);
        readView!.Refresh(message.Entries);
        if (lastShowSequence < 0) readView.Restore(message.RestoreState);
        if (message.CaptureSequence != lastCaptureSequence)
        {
            if (message.CapturePath is { } path) readView.SelectCaptured(path);
            lastCaptureSequence = message.CaptureSequence;
        }
        if (message.StyleFilePath is { } file && readFile is null && !editingPage)
            readFile = file;
        if (message.ShowSequence != lastShowSequence)
        {
            if (message.EditorMode == "edit") SwitchToEdit(message.StyleFilePath);
            else SwitchToRead(message.StyleFilePath);
            SDL_ShowWindow(Window.Handle);
            SDL_RaiseWindow(Window.Handle);
            lastShowSequence = message.ShowSequence;
        }
        if (message.StyleError != lastStyleError)
        {
            lastStyleError = message.StyleError;
            if (message.StyleError is null)
                Window.Title = "文房具UIエディター — " + (readMode ? "読取" : "編集")
                    + (readFile is null ? "（ライブ）" : " — " + Path.GetFileName(readFile));
            else
                Window.Title = "文房具UIエディター — " + (readMode ? "読取" : "編集")
                    + " — アプリの再読込失敗：" + message.StyleError;
        }
    }

    private void RespondLivePacket(GameTime time)
    {
        if (livePacket is { } packet)
        {
            if (!readMode)
                readView!.Update(time, false, new KeyboardState(), new MouseState(),
                    GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            packet.Response.TrySetResult(readView!.Capture());
            livePacket = null;
        }
        if (liveConnection?.Disconnected == true && !liveDisconnectedReported)
        {
            liveDisconnectedReported = true;
            readView?.Restore(readView.Capture() with { CaptureEnabled = false });
            Window.Title = "文房具UIエディター — アプリとの接続が終了（最後の検査結果）";
            liveStyleStatus = "アプリとの接続が終了しました（最後の検査結果）";
        }
    }

    private string GetLiveStyleStatus(DeveloperInspectionMessage packet)
    {
        if (packet.StyleError is not null) return "アプリの再読込失敗：" + packet.StyleError;
        var file = readMode ? readFile : saveSession?.FilePath;
        if (file is null) return "アプリ接続中（比較するファイルなし）";
        if (packet.StyleFilePath is null || !string.Equals(Path.GetFullPath(packet.StyleFilePath), file, StringComparison.OrdinalIgnoreCase))
            return "アプリは別の文房具UIファイルを表示中";
        if (saveSession?.IsDirty == true || invalidDraft || saveError is not null)
            return "編集内容は保存待ち。アプリへの反映も待機中";
        if (packet.AppliedStyleFingerprint is null) return "アプリの反映状態は確認できません";
        try
        {
            var text = File.ReadAllText(file);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            return string.Equals(packet.AppliedStyleFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase)
                ? "アプリへ反映済み" : "保存済み。アプリの再読込を待っています";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return "反映状態を確認できません：" + ex.Message; }
    }

    private void SwitchToEdit(string? path)
    {
        if (readView?.CaptureEnabled == true)
            readView.Restore(readView.Capture() with { CaptureEnabled = false });
        path ??= readFile;
        if (path is null || !File.Exists(path))
        {
            Window.Title = "文房具UIエディター — 編集するファイルがありません";
            return;
        }
        if (saveSession is not null && string.Equals(saveSession.FilePath, path, StringComparison.OrdinalIgnoreCase)) return;
        Guard(() => OpenStyle(path));
    }

    private void SwitchToRead(string? path)
    {
        path ??= string.IsNullOrEmpty(sourceFile) ? readFile : sourceFile;
        if (readMode && string.Equals(readFile, path, StringComparison.OrdinalIgnoreCase)) return;
        if (editingPage && (saveSession?.IsDirty == true || invalidDraft || saveError is not null))
        {
            var answer = System.Windows.Forms.MessageBox.Show("編集内容を保存して読取モードへ切り替えますか？\n「いいえ」は未保存の変更を破棄します。",
                "文房具UIエディター", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel) return;
            if (answer == DialogResult.Yes && !FlushAutoSave()) return;
        }
        if (path is not null && !File.Exists(path)) path = null;
        try { OpenReadStyle(path, discardChanges: true); }
        catch (Exception ex) when (ex is ArgumentException or System.Text.Json.JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Runtime inspection still works when the style document is unreadable.
            OpenReadStyle(null, discardChanges: true);
            Window.Title = "文房具UIエディター — 読取（ライブ）— ファイル読込失敗：" + ex.Message;
        }
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_ShowWindow(nint window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_RaiseWindow(nint window);
}
