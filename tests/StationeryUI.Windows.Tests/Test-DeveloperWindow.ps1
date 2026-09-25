$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class DevNative {
 [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref Point p);
 [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point p);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint x,uint y,uint data,UIntPtr extra);
 [DllImport("user32.dll")] public static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
 public delegate bool Callback(IntPtr hwnd, IntPtr param);
 [DllImport("user32.dll")] public static extern bool EnumWindows(Callback cb,IntPtr p);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h,StringBuilder b,int max);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int cmd);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool PostMessageW(IntPtr h,uint msg,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] public static extern uint MapVirtualKeyW(uint code,uint type);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
 public static IntPtr Find(int pid,string prefix,bool visible) {
  IntPtr result=IntPtr.Zero;
  EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==pid && (!visible||IsWindowVisible(h))) {var b=new StringBuilder(1024);GetWindowTextW(h,b,b.Capacity);if(b.ToString().StartsWith(prefix))result=h;}return true;},IntPtr.Zero);
  return result;
 }
 public static void Key(IntPtr h,uint key,bool down) {
  long bits=1L | ((long)MapVirtualKeyW(key,0)<<16);if(!down)bits|=0xc0000000L;
  PostMessageW(h,down?0x100u:0x101u,(IntPtr)key,(IntPtr)bits);
 }
}
"@
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath = Join-Path $workspacePath 'artifacts/developer-window-stationery'
New-Item -ItemType Directory -Force $outputPath | Out-Null
$reportPath = Join-Path $outputPath 'report.json'
if (Test-Path -LiteralPath $reportPath) { Remove-Item -LiteralPath $reportPath }
Copy-Item -LiteralPath (Join-Path $workspacePath 'App_Data/demo.stationery-ui.json') -Destination (Join-Path $outputPath 'style.json')
'{"styleFile":"style.json","autoReload":true}' | Set-Content (Join-Path $outputPath 'config.json') -Encoding UTF8
$inputPath = Join-Path $outputPath 'input.json'
if (Test-Path -LiteralPath $inputPath) { Remove-Item -LiteralPath $inputPath }
$script:inputSequence = 0
$script:inputX = 0
$script:inputY = 0
$env:STATIONERYUI_SMOKE_PNG = ''
$env:STATIONERYUI_INSPECTOR_TEST_OUTPUT = $outputPath
$env:STATIONERYUI_CONFIG_PATH = Join-Path $outputPath 'config.json'
$demo = Start-Process -FilePath (Join-Path $workspacePath 'samples/StationeryUI.Demo/bin/Release/net8.0-windows/StationeryUI.Demo.exe') -WindowStyle Hidden -PassThru
$inspectorProcess = $null
$cursor = New-Object DevNative+Point
$null = [DevNative]::GetCursorPos([ref]$cursor)
$clipboardBackup = [System.Windows.Forms.Clipboard]::GetDataObject()
function Wait-Window([int]$processId,[string]$prefix,[bool]$visible=$true) {
 $deadline=[DateTime]::UtcNow.AddSeconds(15)
 do { $h=[DevNative]::Find($processId,$prefix,$visible); if($h -ne [IntPtr]::Zero){return $h}; Start-Sleep -Milliseconds 150 } while([DateTime]::UtcNow -lt $deadline)
 throw ('Window missing: '+$prefix)
}
function Send-Input([bool]$down=$false,[int[]]$keys=@()) {
 $script:inputSequence++
 @{Sequence=$script:inputSequence;X=$script:inputX;Y=$script:inputY;Down=$down;Keys=@($keys)} | ConvertTo-Json -Compress | Set-Content -LiteralPath $inputPath -Encoding UTF8
 $null=Wait-Report {param($r) $r.InputSequence -eq $script:inputSequence}
}
function Key([IntPtr]$h,[int]$code) {
 if($h -eq $script:dev -and $script:dev -ne [IntPtr]::Zero) {
  Send-Input $false @($code); Send-Input
 } else {
  [DevNative]::Key($h,$code,$true); Start-Sleep -Milliseconds 100
  [DevNative]::Key($h,$code,$false); Start-Sleep -Milliseconds 200
 }
}
function Activate([IntPtr]$h) {
 $null=[DevNative]::ShowWindow($h,5); $null=[DevNative]::SetForegroundWindow($h); Start-Sleep -Milliseconds 300
}
function Move-Mouse([IntPtr]$h,[int]$x,[int]$y) {
 $point=New-Object DevNative+Point
 $point.X=$x; $point.Y=$y
 $null=[DevNative]::ClientToScreen($h,[ref]$point)
 $null=[DevNative]::SetCursorPos($point.X,$point.Y)
 Start-Sleep -Milliseconds 80
}
function Click([IntPtr]$h,[int]$x,[int]$y) {
 $script:inputX=$x; $script:inputY=$y
 Send-Input
 Send-Input $true
 Send-Input
}
function Report {
 if (Test-Path -LiteralPath $reportPath) {
  try { return Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json } catch { return $null }
 }
 return $null
}
function Wait-Report([scriptblock]$predicate) {
 $deadline=[DateTime]::UtcNow.AddSeconds(15)
 do {
  $r=Report
  if($null -ne $r -and (& $predicate $r)){return $r}
  Start-Sleep -Milliseconds 150
 } while([DateTime]::UtcNow -lt $deadline)
 throw ('Inspector report timeout: ' + (Report | ConvertTo-Json -Depth 5 -Compress))
}
function Select-Path([string]$path) {
 $r=Report
 Activate $dev
 Click $dev ([int]($r.TreeBounds.X+100)) ([int]($r.TreeBounds.Y+20))
 Key $dev 0x24
 $paths=@($r.Rows | ForEach-Object {$_.Path})
 $index=[Array]::IndexOf($paths,$path)
 if($index -lt 0){throw "Missing source path $path"}
 for($i=0;$i -lt $index;$i++){Key $dev 0x28}
 $r=Wait-Report {param($r) $r.State.SelectedPath -eq $path}
 Write-Host "PASS selected $path"
 return $r
}
try {
 $main=Wait-Window $demo.Id 'StationeryUI —' $false
 Activate $main; Key $main 0x7b
 $r=Wait-Report {param($r) $r.State.Visible}
 $inspectorProcess=Get-Process -Id $r.ProcessId
 if($inspectorProcess.Id -eq $demo.Id){throw 'Inspector must have its own MonoGame process'}
 $dev=Wait-Window $inspectorProcess.Id 'F12 開発者ウィンドウ'
 $r=Select-Path '/demo/topDemoPage/body/nameField'
 if(!$r.Details.Contains('種類: textBox')){throw 'Details missing'}
 $style=Get-Content (Join-Path $outputPath 'style.json') -Raw -Encoding UTF8 | ConvertFrom-Json
 ($style.layouts | Where-Object { $_.id -eq 'bodyPadding' }).padding.left='64px'
 $style | ConvertTo-Json -Depth 30 | Set-Content (Join-Path $outputPath 'style.json') -Encoding UTF8
 $r=Wait-Report {param($r) $r.Details.Contains('X=64') -and $r.State.SelectedPath -eq '/demo/topDemoPage/body/nameField'}
 Write-Output 'PASS live coordinates update without losing selection'
 $r=Select-Path '/demo/topDemoPage/body/sampleTree'
 Key $dev 0x25
 $null=Wait-Report {param($r) $r.State.CollapsedPaths -contains '/demo/topDemoPage/body/sampleTree'}
 Write-Output 'PASS StationeryUI tree collapse survives live refresh'
 $r=Select-Path '/demo/topDemoPage/body/editDialog/nameField'
 if(!$r.Details.Contains('非表示')){throw 'Hidden dialog should be marked hidden'}
 Click $dev ([int]($r.CopyBounds.X+80)) ([int]($r.CopyBounds.Y+20))
 $null=Wait-Report {param($r) $r.CopiedPath -eq '/demo/topDemoPage/body/editDialog/nameField'}
 Write-Output 'PASS copy complete selected path'
 $r=Report
 $splitX=$r.TreeBounds.X+$r.TreeBounds.Width+5
 $splitY=$r.SplitBounds.Y+40
 $script:inputX=[int]$splitX; $script:inputY=[int]$splitY
 Send-Input
 Send-Input $true
 $script:inputX=[int]($splitX+140)
 Send-Input $true
 Send-Input
 $r=Wait-Report {param($r) $r.State.SplitRatio -gt .5}
 Write-Output 'PASS StationeryUI split divider drag'
 $r=Report
 $script:inputX=[int]($r.DetailsBounds.X+$r.DetailsBounds.Width-8)
 $script:inputY=[int]($r.DetailsBounds.Y+20)
 Send-Input
 Send-Input $true
 $script:inputY=[int]($r.DetailsBounds.Y+$r.DetailsBounds.Height+50)
 Send-Input $true
 Send-Input
 $r=Wait-Report {param($r) $r.DetailsScroll -gt 0}
 Write-Output 'PASS read-only details scrollbar drag'
 '' | Set-Content (Join-Path $outputPath 'capture.request')
 Start-Sleep -Milliseconds 500
 if(!(Test-Path (Join-Path $outputPath 'inspector.png'))){throw 'Inspector screenshot missing'}
 Key $dev 0x7b
 $null=Wait-Report {param($r) !$r.State.Visible}
 Activate $main; Key $main 0x7b
 $r=Wait-Report {param($r) $r.State.Visible}
 if($r.ProcessId -ne $inspectorProcess.Id -or $r.State.SplitRatio -le .5){throw 'Reopen lost inspector state'}
 Write-Output 'PASS F12 hide/reopen retains the same window and split ratio'
 Key $dev 0x1b
 $null=Wait-Report {param($r) !$r.State.Visible}
 Activate $main; Key $main 0x7b
 $null=Wait-Report {param($r) $r.State.Visible}
 $oldInspectorId=$inspectorProcess.Id
 $null=[DevNative]::PostMessageW($dev,0x10,[IntPtr]::Zero,[IntPtr]::Zero)
 if(!$inspectorProcess.WaitForExit(10000)){throw 'Close button did not exit inspector'}
 Start-Sleep -Milliseconds 500
 Activate $main; Key $main 0x7b
 $r=Wait-Report {param($r) $r.State.Visible -and $r.ProcessId -ne $oldInspectorId}
 $inspectorProcess=Get-Process -Id $r.ProcessId
 $script:dev=Wait-Window $inspectorProcess.Id 'F12 開発者ウィンドウ' $false
 if($r.State.SplitRatio -le .5 -or $r.State.SelectedPath -ne '/demo/topDemoPage/body/editDialog/nameField'){throw 'Close/reopen lost view state'}
 Write-Output 'PASS close button and new process restore view state'
 $null=[DevNative]::PostMessageW($main,0x10,[IntPtr]::Zero,[IntPtr]::Zero)
 if(!$demo.WaitForExit(10000)){throw 'Demo did not exit'}
 if(!$inspectorProcess.WaitForExit(10000)){throw 'Inspector process survived parent shutdown'}
 if($demo.ExitCode -ne 0){throw "Demo exit code $($demo.ExitCode)"}
 Write-Output 'PASS Esc, reopen, and parent shutdown'
} finally {
 [DevNative]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
 $null=[DevNative]::SetCursorPos($cursor.X,$cursor.Y)
 if($null -ne $clipboardBackup){[System.Windows.Forms.Clipboard]::SetDataObject($clipboardBackup,$true)}
 if(!$demo.HasExited){$demo.Kill();$demo.WaitForExit()}
 if($null -ne $inspectorProcess -and !$inspectorProcess.HasExited){$inspectorProcess.Kill();$inspectorProcess.WaitForExit()}
}
