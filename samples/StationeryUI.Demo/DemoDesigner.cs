using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

internal sealed partial class Demo
{
    private void OpenLayoutDesigner()
    {
        try
        {
            var file = Path.GetFullPath(styles.FilePath);
            if (!File.Exists(file)) throw new FileNotFoundException("文房具UIファイルが見つかりません。", file);
            var executable = Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_PATH");
            if (string.IsNullOrWhiteSpace(executable))
            {
                var adjacent = Path.Combine(AppContext.BaseDirectory, "StationeryUI.StyleDesigner.exe");
                executable = File.Exists(adjacent) ? adjacent : Assembly.GetExecutingAssembly()
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(attribute => attribute.Key == "StationeryDesignerPath")?.Value;
            }
            if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
                throw new FileNotFoundException("レイアウトデザイナーが見つかりません。STATIONERYUI_DESIGNER_PATH に実行ファイルを指定してください。");
            var start = new ProcessStartInfo(Path.GetFullPath(executable)) { UseShellExecute = false };
            start.ArgumentList.Add(file);
            using var process = Process.Start(start);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            Trace.WriteLine(ex);
            Window.Title = "レイアウトデザイナー起動失敗 — " + ex.Message;
        }
    }
}
