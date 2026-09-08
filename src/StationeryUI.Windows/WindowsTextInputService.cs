namespace StationeryUI.Windows;

using System.Runtime.InteropServices;
using System.Text;
using StationeryUI.Canvas;
using StationeryUI.Text;

/// <summary>DesktopGL/SDL2 text input. Construct on the game thread with GameWindow.Handle (SDL_Window*).</summary>
public sealed class WindowsTextInputService : ITextInputService
{
    private static WindowsTextInputService? active;
    private readonly object gate = new();
    private readonly Queue<TextInputUpdate> updates = new();
    private readonly EventWatch watch;
    private readonly uint windowId;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private bool started;
    private bool disposed;
    public long DroppedEventCount { get; private set; }

    public WindowsTextInputService(nint sdlWindow)
    {
        if (sdlWindow == 0) throw new ArgumentException("An SDL window is required.", nameof(sdlWindow));
        windowId = SDL_GetWindowID(sdlWindow);
        if (windowId == 0) throw new ArgumentException("Invalid SDL window.", nameof(sdlWindow));
        watch = Watch;
    }
    private void CheckThread()
    {
        if (ownerThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Use the game thread.");
    }
    public void Start()
    {
        CheckThread();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (started) return;
        // SDL2 has one process-wide text input session. Do not silently steal it.
        if (Interlocked.CompareExchange(ref active, this, null) is not null)
            throw new InvalidOperationException("Another StationeryUI text input session is active.");
        try
        {
            SDL_SetHint("SDL_IME_SHOW_UI", "1");
            lock (gate) { updates.Clear(); started = true; }
            SDL_AddEventWatch(watch, 0);
            SDL_StartTextInput();
        }
        catch
        {
            lock (gate) started = false;
            SDL_DelEventWatch(watch, 0);
            Interlocked.CompareExchange(ref active, null, this);
            throw;
        }
    }
    public void Stop()
    {
        CheckThread();
        if (!started) return;
        lock (gate) { started = false; updates.Clear(); }
        try { SDL_DelEventWatch(watch, 0); SDL_StopTextInput(); }
        finally { Interlocked.CompareExchange(ref active, null, this); }
    }
    public void SetInputArea(ScreenRectangle area)
    {
        CheckThread();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!double.IsFinite(area.X) || !double.IsFinite(area.Y) || !double.IsFinite(area.Width) ||
            !double.IsFinite(area.Height) || area.Width <= 0 || area.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(area));
        if (!started) return;
        var rectangle = new SdlRectangle { X = checked((int)Math.Floor(area.X)), Y = checked((int)Math.Floor(area.Y)),
            W = checked((int)Math.Ceiling(area.Width)), H = checked((int)Math.Ceiling(area.Height)) };
        SDL_SetTextInputRect(ref rectangle);
    }
    public IReadOnlyList<TextInputUpdate> DrainUpdates()
    {
        CheckThread();
        lock (gate) { var result = updates.ToArray(); updates.Clear(); return result; }
    }
    private int Watch(nint _, nint data)
    {
        try
        {
            var type = Marshal.ReadInt32(data);
            if (type is not (0x302 or 0x303 or 0x305) || unchecked((uint)Marshal.ReadInt32(data, 8)) != windowId) return 0;
            string text;
            var caret = 0;
            var length = 0;
            if (type == 0x305)
            {
                var pointerOffset = IntPtr.Size == 8 ? 16 : 12;
                // Event watches only borrow this buffer; the event consumer owns SDL_free.
                text = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(data, pointerOffset)) ?? "";
                caret = Marshal.ReadInt32(data, pointerOffset + IntPtr.Size);
                length = Marshal.ReadInt32(data, pointerOffset + IntPtr.Size + 4);
            }
            else
            {
                var bytes = new byte[32];
                Marshal.Copy(data + 12, bytes, 0, bytes.Length);
                var end = Array.IndexOf(bytes, (byte)0);
                text = Encoding.UTF8.GetString(bytes, 0, end < 0 ? bytes.Length : end);
                if (type == 0x302) { caret = Marshal.ReadInt32(data, 44); length = Marshal.ReadInt32(data, 48); }
            }
            var start = Utf16Index(text, caret);
            var finish = Utf16Index(text, Math.Max(0, caret) + Math.Max(0, length));
            lock (gate)
            {
                if (started) updates.Enqueue(new(text, type != 0x303, start, Math.Max(0, finish - start)));
            }
        }
        catch { lock (gate) DroppedEventCount++; } // Never propagate into SDL's unmanaged callback.
        return 0;
    }
    private static int Utf16Index(string value, int codePoints)
    {
        var offset = 0;
        foreach (var rune in value.EnumerateRunes()) { if (codePoints-- <= 0) break; offset += rune.Utf16SequenceLength; }
        return offset;
    }
    public string ReadClipboard() { CheckThread(); return System.Windows.Forms.Clipboard.ContainsText() ? System.Windows.Forms.Clipboard.GetText() : ""; }
    public void WriteClipboard(string text) { CheckThread(); if (text.Length > 0) System.Windows.Forms.Clipboard.SetText(text); }
    public void Dispose() { if (disposed) return; Stop(); disposed = true; }
    [StructLayout(LayoutKind.Sequential)] private struct SdlRectangle { public int X, Y, W, H; }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int EventWatch(nint userData, nint data);
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)] private static extern uint SDL_GetWindowID(nint window);
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_AddEventWatch(EventWatch callback, nint userData);
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_DelEventWatch(EventWatch callback, nint userData);
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_StartTextInput();
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_StopTextInput();
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_SetTextInputRect(ref SdlRectangle rectangle);
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_SetHint([MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
}
