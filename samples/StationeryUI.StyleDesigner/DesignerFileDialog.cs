using StationeryUI.Windows;
using System.Runtime.InteropServices;
using System.Text;

internal sealed partial class DesignerGame
{
    private bool testedNativeDialog;
    private string? ChooseStyleFile()
    {
        var test = !string.IsNullOrEmpty(smokeOutput) && !string.IsNullOrEmpty(smokeInput);
        var nativeTest = test && Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_NATIVE_DIALOG") == "1";
        if (test && !nativeTest) return smokeInput;
        // Native-dialog smoke mode accepts the preselected fixture in this process's dialog only.
        // Normal application runs have neither a timer nor synthetic native input.
        using var timer = new System.Windows.Forms.Timer { Interval = 300 };
        if (nativeTest)
        {
            var observedTicks = 0;
            timer.Tick += (_, _) =>
            {
                var window = GetForegroundWindow();
                GetWindowThreadProcessId(window, out var process);
                var name = new StringBuilder(128); GetClassName(window, name, name.Capacity);
                if (process != Environment.ProcessId || name.ToString() != "#32770") return;
                if (++observedTicks < 3) return;
                testedNativeDialog = true; timer.Stop();
                var command = Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_CANCEL_DIALOG") == "1" ? 2 : 1;
                var button = GetDlgItem(window, command);
                if (button != 0) PostMessage(button, 0x00F5, 0, 0); // BM_CLICK
                else PostMessage(window, 0x0111, command, 0); // WM_COMMAND
            };
            timer.Start();
        }
        return WindowsStyleFileDialog.Open(nativeTest ? smokeInput : sourceFile);
    }

    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetDlgItem(nint window, int id);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out int process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder name, int maximum);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
