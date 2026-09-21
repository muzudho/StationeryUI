namespace StationeryUI.Windows;

using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>Native Windows file selection, owned by the calling UI thread's active HWND.</summary>
public static class WindowsStyleFileDialog
{
    public static string? ChooseFolder(string? initialFolder = null)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "新しいスタイル設定ファイルのフォルダーを選択",
            UseDescriptionForTitle = true,
            SelectedPath = initialFolder ?? "",
            ShowNewFolderButton = true
        };
        var owner = GetActiveWindow();
        var result = owner == 0 ? dialog.ShowDialog() : dialog.ShowDialog(new WindowOwner(owner));
        return result == DialogResult.OK ? dialog.SelectedPath : null;
    }

    public static string? Open(string? initialFile = null)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "既存のスタイル設定ファイルを選択",
            Filter = "文房具 UI スタイル (*.stationery-style.json)|*.stationery-style.json|JSON ファイル (*.json)|*.json",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            RestoreDirectory = true
        };
        if (!string.IsNullOrWhiteSpace(initialFile))
        {
            var full = Path.GetFullPath(initialFile);
            if (Directory.Exists(Path.GetDirectoryName(full))) dialog.InitialDirectory = Path.GetDirectoryName(full);
            if (File.Exists(full)) dialog.FileName = full;
        }
        // GameWindow.Handle is an SDL pointer, not a Win32 HWND. Obtain the actual active owner.
        var owner = GetActiveWindow();
        var result = owner == 0 ? dialog.ShowDialog() : dialog.ShowDialog(new WindowOwner(owner));
        return result == DialogResult.OK ? dialog.FileName : null;
    }

    private sealed class WindowOwner(nint handle) : IWin32Window { public nint Handle => handle; }
    [DllImport("user32.dll")] private static extern nint GetActiveWindow();
}
