namespace StationeryUI.Windows;

using StationeryUI.Platform;
using System;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Windows のクリップボードへテキストを書き込みます。
/// </summary>
public sealed class WindowsClipboardService : IClipboardService
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    public bool TrySetText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (!OpenClipboard(IntPtr.Zero)) return false;

        var clipboardData = IntPtr.Zero;
        try
        {
            EmptyClipboard();

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            clipboardData = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
            if (clipboardData == IntPtr.Zero) return false;

            var lockedMemory = GlobalLock(clipboardData);
            if (lockedMemory == IntPtr.Zero) return false;

            try
            {
                Marshal.Copy(bytes, 0, lockedMemory, bytes.Length);
            }
            finally
            {
                GlobalUnlock(clipboardData);
            }

            if (SetClipboardData(CfUnicodeText, clipboardData) == IntPtr.Zero) return false;

            clipboardData = IntPtr.Zero;
            return true;
        }
        finally
        {
            if (clipboardData != IntPtr.Zero)
            {
                GlobalFree(clipboardData);
            }

            CloseClipboard();
        }
    }

    public bool TryGetText(out string text)
    {
        text = "";
        if (!OpenClipboard(IntPtr.Zero)) return false;
        try
        {
            var clipboardData = GetClipboardData(CfUnicodeText);
            if (clipboardData == IntPtr.Zero) return false;
            var lockedMemory = GlobalLock(clipboardData);
            if (lockedMemory == IntPtr.Zero) return false;
            try
            {
                text = Marshal.PtrToStringUni(lockedMemory) ?? "";
                return true;
            }
            finally
            {
                GlobalUnlock(clipboardData);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
