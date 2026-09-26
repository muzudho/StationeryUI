using StationeryUI.Windows;
using System.Runtime.InteropServices;
using System.Text;

internal sealed partial class EditorGame
{
    private bool testedNativeDialog;
    private string? ChooseStyleFile()
    {
        var test = !string.IsNullOrEmpty(smokeOutput) && !string.IsNullOrEmpty(smokeInput);
        var nativeTest = test && Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_NATIVE_DIALOG") == "1";
        if (test && !nativeTest) return smokeInput;
        // Native-dialog smoke mode accepts the preselected fixture in this process's dialog only.
        // Normal application runs have neither a timer nor synthetic native input.
        using var timer = new System.Windows.Forms.Timer { Interval = 300 };
        if (nativeTest)
        {
            var observedTicks = 0;
            timer.Tick += (_, _) =>
            {
                nint window = 0;
                EnumWindows((candidate, _) =>
                {
                    GetWindowThreadProcessId(candidate, out var process);
                    if (process != Environment.ProcessId) return true;
                    var name = new StringBuilder(128);
                    GetClassName(candidate, name, name.Capacity);
                    if (name.ToString() != "#32770") return true;
                    window = candidate;
                    return false;
                }, 0);
                if (window == 0) return;
                if (++observedTicks < 3) return;
                testedNativeDialog = true; timer.Stop();
                var command = Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_CANCEL_DIALOG") == "1" ? 2 : 1;
                var button = GetDlgItem(window, command);
                if (button != 0) PostMessage(button, 0x00F5, 0, 0); // BM_CLICK
                else PostMessage(window, 0x0111, command, 0); // WM_COMMAND
            };
            timer.Start();
        }
        return WindowsStyleFileDialog.Open(nativeTest ? smokeInput : sourceFile);
    }

    private delegate bool EnumWindowsCallback(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetDlgItem(nint window, int id);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out int process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder name, int maximum);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
