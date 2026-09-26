using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

internal sealed partial class Demo
{
    private void OpenUiEditor()
    {
        try
        {
            var file = Path.GetFullPath(styles.FilePath);
            if (!File.Exists(file)) throw new FileNotFoundException("文房具UIファイルが見つかりません。", file);
            var executable = Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_PATH");
            if (string.IsNullOrWhiteSpace(executable))
                executable = Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_PATH");
            if (string.IsNullOrWhiteSpace(executable))
            {
                var adjacent = Path.Combine(AppContext.BaseDirectory, "StationeryUIEditor.exe");
                executable = File.Exists(adjacent) ? adjacent : Assembly.GetExecutingAssembly()
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(attribute => attribute.Key == "StationeryEditorPath")?.Value;
            }
            if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
                throw new FileNotFoundException("文房具UIエディターが見つかりません。STATIONERYUI_EDITOR_PATH に実行ファイルを指定してください。");
            var start = new ProcessStartInfo(Path.GetFullPath(executable)) { UseShellExecute = false };
            start.ArgumentList.Add(file);
            using var process = Process.Start(start);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            Trace.WriteLine(ex);
            Window.Title = "文房具UIエディター起動失敗 — " + ex.Message;
        }
    }
}
