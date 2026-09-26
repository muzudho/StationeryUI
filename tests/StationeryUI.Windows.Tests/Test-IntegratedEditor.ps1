$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class IntegratedEditorNative {
 public delegate bool Callback(IntPtr hwnd, IntPtr param);
 [DllImport("user32.dll")] public static extern bool EnumWindows(Callback cb, IntPtr p);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr hwnd, StringBuilder title, int max);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int cmd);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern bool PostMessageW(IntPtr hwnd, uint message, IntPtr key, IntPtr data);
 [DllImport("user32.dll")] public static extern uint MapVirtualKeyW(uint code, uint type);
 public static IntPtr Find(int pid, string prefix) {
  IntPtr found=IntPtr.Zero;
  EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==pid){var title=new StringBuilder(1024);GetWindowTextW(h,title,title.Capacity);if(title.ToString().StartsWith(prefix))found=h;}return true;},IntPtr.Zero);
  return found;
 }
 public static IntPtr FindEditor(int pid) { return Find(pid,"\u6587\u623f\u5177UI\u30a8\u30c7\u30a3\u30bf\u30fc"); }
 public static bool IsRead(IntPtr hwnd) { return Title(hwnd).Contains("\u8aad\u53d6"); }
 public static bool IsEdit(IntPtr hwnd) { return Title(hwnd).Contains("\u7de8\u96c6"); }
 public static bool IsDisconnected(IntPtr hwnd) { return Title(hwnd).Contains("\u63a5\u7d9a\u304c\u7d42\u4e86"); }
 public static string Title(IntPtr hwnd) { var title=new StringBuilder(1024);GetWindowTextW(hwnd,title,title.Capacity);return title.ToString(); }
 public static string Titles(int pid) { var result=new StringBuilder();EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==pid){var title=new StringBuilder(1024);GetWindowTextW(h,title,title.Capacity);result.Append('[').Append(title).Append(']');}return true;},IntPtr.Zero);return result.ToString(); }
 public static void Key(IntPtr hwnd, int key, bool down) {
  long bits=1L | ((long)MapVirtualKeyW((uint)key,0)<<16);if(!down)bits|=0xc0000000L;
  PostMessageW(hwnd,down?0x100u:0x101u,(IntPtr)key,(IntPtr)bits);
 }
}
"@
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $workspace ('artifacts/integrated-editor/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$style = Join-Path $output 'demo.stationery-ui.json'
Copy-Item -LiteralPath (Join-Path $workspace 'App_Data/demo.stationery-ui.json') -Destination $style
$before = (Get-FileHash -LiteralPath $style).Hash
$beforeWrite = (Get-Item -LiteralPath $style).LastWriteTimeUtc
'{"styleFile":"demo.stationery-ui.json","autoReload":true}' | Set-Content -LiteralPath (Join-Path $output 'config.json') -Encoding UTF8
$previousConfig = $env:STATIONERYUI_CONFIG_PATH
$previousInspector = $env:STATIONERYUI_INSPECTOR_TEST_OUTPUT
$previousCrashLog = $env:STATIONERYUI_EDITOR_CRASH_LOG
$env:STATIONERYUI_CONFIG_PATH = Join-Path $output 'config.json'
$env:STATIONERYUI_INSPECTOR_TEST_OUTPUT = ''
$env:STATIONERYUI_EDITOR_CRASH_LOG = Join-Path $output 'editor-error.txt'
$oldEditors = @(Get-Process StationeryUIEditor -ErrorAction SilentlyContinue | ForEach-Object Id)
$demo = $null
$editor = $null
try {
 $demo = Start-Process -FilePath (Join-Path $workspace 'samples/StationeryUI.Demo/bin/Release/net8.0-windows/StationeryUI.Demo.exe') -WindowStyle Hidden -PassThru
 $deadline = [DateTime]::UtcNow.AddSeconds(15)
 do { $demoWindow = [IntegratedEditorNative]::Find($demo.Id,'StationeryUI'); if ($demoWindow -ne [IntPtr]::Zero) { break }; Start-Sleep -Milliseconds 100 } while ([DateTime]::UtcNow -lt $deadline)
 if ($demoWindow -eq [IntPtr]::Zero) { throw 'Demo window missing.' }
 $null = [IntegratedEditorNative]::ShowWindow($demoWindow,5)
 $null = [IntegratedEditorNative]::SetForegroundWindow($demoWindow)
 Start-Sleep -Milliseconds 300
 [IntegratedEditorNative]::Key($demoWindow,123,$true); Start-Sleep -Milliseconds 120
 [IntegratedEditorNative]::Key($demoWindow,123,$false)
 $deadline = [DateTime]::UtcNow.AddSeconds(20)
 do {
  $editor = Get-Process StationeryUIEditor -ErrorAction SilentlyContinue | Where-Object { $oldEditors -notcontains $_.Id } | Select-Object -First 1
  if ($editor -and [IntegratedEditorNative]::FindEditor($editor.Id) -ne [IntPtr]::Zero) { break }
  Start-Sleep -Milliseconds 150
 } while ([DateTime]::UtcNow -lt $deadline)
 if (!$editor -or [IntegratedEditorNative]::FindEditor($editor.Id) -eq [IntPtr]::Zero) { throw 'F12 did not start the integrated editor.' }
 Start-Sleep -Seconds 2
 if (![IntegratedEditorNative]::IsRead([IntegratedEditorNative]::FindEditor($editor.Id))) { throw 'F12 editor did not enter read mode.' }
 if (Get-ChildItem -LiteralPath $output -Filter '*.bak') { throw 'F12 read mode created a backup.' }
 if ((Get-FileHash -LiteralPath $style).Hash -ne $before) { throw 'F12 read mode changed the file.' }
 if ((Get-Item -LiteralPath $style).LastWriteTimeUtc -ne $beforeWrite) { throw 'F12 read mode changed the modification time.' }
 Write-Output 'PASS F12 opens read-only integrated editor'
 $null = [IntegratedEditorNative]::SetForegroundWindow($demoWindow)
 Start-Sleep -Milliseconds 300
 [IntegratedEditorNative]::Key($demoWindow,17,$true); Start-Sleep -Milliseconds 80
 [IntegratedEditorNative]::Key($demoWindow,123,$true); Start-Sleep -Milliseconds 120
 [IntegratedEditorNative]::Key($demoWindow,123,$false)
 [IntegratedEditorNative]::Key($demoWindow,17,$false)
 $deadline = [DateTime]::UtcNow.AddSeconds(15)
 do { if (Test-Path -LiteralPath ($style + '.1.bak')) { break }; Start-Sleep -Milliseconds 150 } while ([DateTime]::UtcNow -lt $deadline)
 if (!(Test-Path -LiteralPath ($style + '.1.bak'))) { throw 'Ctrl+F12 did not enter edit mode.' }
 $deadline = [DateTime]::UtcNow.AddSeconds(10)
 do { if ([IntegratedEditorNative]::IsEdit([IntegratedEditorNative]::FindEditor($editor.Id))) { break }; Start-Sleep -Milliseconds 150 } while ([DateTime]::UtcNow -lt $deadline)
 if (![IntegratedEditorNative]::IsEdit([IntegratedEditorNative]::FindEditor($editor.Id))) { throw 'Ctrl+F12 editor did not show edit mode.' }
 $newEditors = @(Get-Process StationeryUIEditor -ErrorAction SilentlyContinue | Where-Object { $oldEditors -notcontains $_.Id })
 if ($newEditors.Count -ne 1 -or $newEditors[0].Id -ne $editor.Id) { throw 'Mode switch created a second editor.' }
 Write-Output 'PASS Ctrl+F12 reuses the editor and enables editing'
 $null = [IntegratedEditorNative]::SetForegroundWindow($demoWindow)
 Start-Sleep -Milliseconds 300
 [IntegratedEditorNative]::Key($demoWindow,123,$true); Start-Sleep -Milliseconds 120
 [IntegratedEditorNative]::Key($demoWindow,123,$false)
 $deadline = [DateTime]::UtcNow.AddSeconds(10)
 do {
  $editorWindow = [IntegratedEditorNative]::FindEditor($editor.Id)
  if ($editorWindow -ne [IntPtr]::Zero -and [IntegratedEditorNative]::IsRead($editorWindow)) { break }
  Start-Sleep -Milliseconds 150
 } while ([DateTime]::UtcNow -lt $deadline)
 if ($editorWindow -eq [IntPtr]::Zero -or ![IntegratedEditorNative]::IsRead($editorWindow)) {
  $errorText = if (Test-Path -LiteralPath $env:STATIONERYUI_EDITOR_CRASH_LOG) { Get-Content -LiteralPath $env:STATIONERYUI_EDITOR_CRASH_LOG -Raw } else { '' }
  throw ('F12 did not return to read mode. EditorExited=' + $editor.HasExited + ' Titles=' + [IntegratedEditorNative]::Titles($editor.Id) + ' Error=' + $errorText)
 }
 if ((Get-FileHash -LiteralPath $style).Hash -ne $before) { throw 'Mode switch changed the file.' }
 Write-Output 'PASS F12 returns the same editor to read mode'
 $demo.Kill(); $demo.WaitForExit()
 $deadline = [DateTime]::UtcNow.AddSeconds(10)
 do { if ([IntegratedEditorNative]::IsDisconnected([IntegratedEditorNative]::FindEditor($editor.Id))) { break }; Start-Sleep -Milliseconds 150 } while ([DateTime]::UtcNow -lt $deadline)
 if ($editor.HasExited -or ![IntegratedEditorNative]::IsDisconnected([IntegratedEditorNative]::FindEditor($editor.Id))) { throw 'Editor did not retain the last inspection after host exit.' }
 Write-Output 'PASS editor remains open with last inspection after host exit'
} finally {
 if ($demo -and !$demo.HasExited) { $demo.Kill(); $demo.WaitForExit() }
 if ($editor -and !$editor.HasExited) { $editor.Kill(); $editor.WaitForExit() }
 $env:STATIONERYUI_CONFIG_PATH = $previousConfig
 $env:STATIONERYUI_INSPECTOR_TEST_OUTPUT = $previousInspector
 $env:STATIONERYUI_EDITOR_CRASH_LOG = $previousCrashLog
}
