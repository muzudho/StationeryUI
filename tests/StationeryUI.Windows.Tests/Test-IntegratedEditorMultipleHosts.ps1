$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class MultipleHostEditorNative {
 public delegate bool Callback(IntPtr hwnd, IntPtr param);
 [DllImport("user32.dll")] public static extern bool EnumWindows(Callback callback, IntPtr param);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr hwnd, StringBuilder title, int max);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int command);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern bool PostMessageW(IntPtr hwnd, uint message, IntPtr key, IntPtr data);
 [DllImport("user32.dll")] public static extern uint MapVirtualKeyW(uint code, uint type);
 public static IntPtr Find(int pid, string prefix) {
  IntPtr found=IntPtr.Zero;
  EnumWindows((hwnd,param)=>{uint owner;GetWindowThreadProcessId(hwnd,out owner);
   if(owner==pid){var title=new StringBuilder(1024);GetWindowTextW(hwnd,title,title.Capacity);
    if(title.ToString().StartsWith(prefix))found=hwnd;}return true;},IntPtr.Zero);
  return found;
 }
 public static string Title(IntPtr hwnd) { var text=new StringBuilder(1024);GetWindowTextW(hwnd,text,text.Capacity);return text.ToString(); }
 public static void Key(IntPtr hwnd, int key, bool down) {
  long bits=1L | ((long)MapVirtualKeyW((uint)key,0)<<16);if(!down)bits|=0xc0000000L;
  PostMessageW(hwnd,down?0x100u:0x101u,(IntPtr)key,(IntPtr)bits);
 }
}
"@
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $workspace ('artifacts/integrated-editor-multiple-hosts/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$previousConfig = $env:STATIONERYUI_CONFIG_PATH
$previousInspector = $env:STATIONERYUI_INSPECTOR_TEST_OUTPUT
$previousEditorOutput = $env:STATIONERYUI_EDITOR_TEST_OUTPUT
$oldEditors = @(Get-Process StationeryUIEditor -ErrorAction SilentlyContinue | ForEach-Object Id)
$hosts = @()
$editors = @()
try {
 $env:STATIONERYUI_INSPECTOR_TEST_OUTPUT = ''
 $env:STATIONERYUI_EDITOR_TEST_OUTPUT = ''
 for ($i = 1; $i -le 2; $i++) {
  $directory = Join-Path $output "host$i"
  New-Item -ItemType Directory -Force -Path $directory | Out-Null
  $style = Join-Path $directory 'demo.stationery-ui.json'
  Copy-Item -LiteralPath (Join-Path $workspace 'App_Data/demo.stationery-ui.json') -Destination $style
  '{"styleFile":"demo.stationery-ui.json","autoReload":true}' | Set-Content -LiteralPath (Join-Path $directory 'config.json') -Encoding UTF8
  $env:STATIONERYUI_CONFIG_PATH = Join-Path $directory 'config.json'
  $hostProcess = Start-Process -FilePath (Join-Path $workspace 'samples/StationeryUI.Demo/bin/Release/net8.0-windows/StationeryUI.Demo.exe') -WindowStyle Hidden -PassThru
  $hosts += $hostProcess
  $deadline = [DateTime]::UtcNow.AddSeconds(15)
  do { $window = [MultipleHostEditorNative]::Find($hostProcess.Id, 'StationeryUI'); if ($window -ne [IntPtr]::Zero) { break }; Start-Sleep -Milliseconds 100 } while ([DateTime]::UtcNow -lt $deadline)
  if ($window -eq [IntPtr]::Zero) { throw "Host $i did not open." }
  $null = [MultipleHostEditorNative]::ShowWindow($window, 5)
  $null = [MultipleHostEditorNative]::SetForegroundWindow($window)
  Start-Sleep -Milliseconds 300
  [MultipleHostEditorNative]::Key($window, 123, $true); Start-Sleep -Milliseconds 120
  [MultipleHostEditorNative]::Key($window, 123, $false)
  $deadline = [DateTime]::UtcNow.AddSeconds(20)
  do {
   $editor = Get-Process StationeryUIEditor -ErrorAction SilentlyContinue | Where-Object { $oldEditors -notcontains $_.Id -and $editors.Id -notcontains $_.Id } | Select-Object -First 1
   if ($editor -and [MultipleHostEditorNative]::Find($editor.Id, '文房具UIエディター') -ne [IntPtr]::Zero) { break }
   Start-Sleep -Milliseconds 150
  } while ([DateTime]::UtcNow -lt $deadline)
  if (!$editor) { throw "Host $i did not start an editor." }
  $editors += $editor
 }
 if ($editors[0].Id -eq $editors[1].Id) { throw 'Two hosts shared one editor process.' }
 $secondHost = [MultipleHostEditorNative]::Find($hosts[1].Id, 'StationeryUI')
 $null = [MultipleHostEditorNative]::SetForegroundWindow($secondHost)
 Start-Sleep -Milliseconds 300
 [MultipleHostEditorNative]::Key($secondHost, 17, $true); Start-Sleep -Milliseconds 80
 [MultipleHostEditorNative]::Key($secondHost, 123, $true); Start-Sleep -Milliseconds 120
 [MultipleHostEditorNative]::Key($secondHost, 123, $false)
 [MultipleHostEditorNative]::Key($secondHost, 17, $false)
 $secondStyle = Join-Path $output 'host2/demo.stationery-ui.json'
 $deadline = [DateTime]::UtcNow.AddSeconds(15)
 do { if (Test-Path -LiteralPath ($secondStyle + '.1.bak')) { break }; Start-Sleep -Milliseconds 150 } while ([DateTime]::UtcNow -lt $deadline)
 if (!(Test-Path -LiteralPath ($secondStyle + '.1.bak'))) { throw 'Second host did not open its own file for editing.' }
 if (Test-Path -LiteralPath (Join-Path $output 'host1/demo.stationery-ui.json.1.bak')) { throw 'Second host edited the first host file.' }
 $hosts[0].Kill(); $hosts[0].WaitForExit()
 $deadline = [DateTime]::UtcNow.AddSeconds(10)
 do {
  $firstTitle = [MultipleHostEditorNative]::Title([MultipleHostEditorNative]::Find($editors[0].Id, '文房具UIエディター'))
  if ($firstTitle.Contains('接続が終了')) { break }
  Start-Sleep -Milliseconds 150
 } while ([DateTime]::UtcNow -lt $deadline)
 $secondTitle = [MultipleHostEditorNative]::Title([MultipleHostEditorNative]::Find($editors[1].Id, '文房具UIエディター'))
 if (!$firstTitle.Contains('接続が終了') -or $secondTitle.Contains('接続が終了') -or $editors[0].HasExited -or $editors[1].HasExited) {
  throw "Host connections were mixed. First='$firstTitle' Second='$secondTitle'"
 }
 Write-Output 'PASS two hosts keep separate editor processes, style files and disconnect state'
} finally {
 foreach ($hostProcess in $hosts) { if (!$hostProcess.HasExited) { $hostProcess.Kill(); $hostProcess.WaitForExit() } }
 foreach ($editor in $editors) { if (!$editor.HasExited) { $editor.Kill(); $editor.WaitForExit() } }
 $env:STATIONERYUI_CONFIG_PATH = $previousConfig
 $env:STATIONERYUI_INSPECTOR_TEST_OUTPUT = $previousInspector
 $env:STATIONERYUI_EDITOR_TEST_OUTPUT = $previousEditorOutput
}
