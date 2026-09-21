$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class DevNative {
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
New-Item -ItemType Directory -Force (Join-Path $workspacePath 'artifacts/developer-window') | Out-Null
$env:STATIONERYUI_SMOKE_PNG = ''
$env:STATIONERYUI_CONFIG_PATH = ''
$demo = Start-Process -FilePath (Join-Path $workspacePath 'samples\StationeryUI.Demo\bin\Release\net8.0-windows\StationeryUI.Demo.exe') -WindowStyle Hidden -PassThru
function Wait-Window([string]$prefix,[bool]$visible=$true) {
 $deadline=[DateTime]::UtcNow.AddSeconds(15)
 do { $h=[DevNative]::Find($demo.Id,$prefix,$visible); if($h -ne [IntPtr]::Zero){return $h}; Start-Sleep -Milliseconds 150 } while([DateTime]::UtcNow -lt $deadline)
 throw ('Window missing: '+$prefix)
}
function Press-F12([IntPtr]$h) {
 $null=[DevNative]::ShowWindow($h,5)
 $null=[DevNative]::SetForegroundWindow($h)
 Start-Sleep -Milliseconds 400
 [DevNative]::Key($h,0x7b,$true)
 Start-Sleep -Milliseconds 200
 [DevNative]::Key($h,0x7b,$false)
}
try {
 $main=Wait-Window 'StationeryUI —' $false
 Press-F12 $main
 $dev=Wait-Window 'F12 開発者ウィンドウ'
 Start-Sleep -Milliseconds 700
 $automation=[System.Windows.Automation.AutomationElement]::FromHandle($dev)
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::TreeItem)
 $items=$automation.FindAll([System.Windows.Automation.TreeScope]::Descendants,$condition)
 $names=@($items | ForEach-Object {$_.Current.Name})
 $names | Write-Output
 $fields=@($items | Where-Object {$_.Current.Name.StartsWith('nameField ')})
 if($fields.Count -ne 2){throw 'Expected two nameField nodes'}
 $textCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Edit)
 $details=$automation.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$textCondition)
 $paths=@('/demo/topDemoPage/nameField','/demo/topDemoPage/editDialog/nameField')
 for($i=0;$i -lt 2;$i++) {
  $fields[$i].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
  Start-Sleep -Milliseconds 250
  $value=$details.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
  if(!$value.Contains($paths[$i])){throw ('Path missing: '+$paths[$i]+' actual='+$value)}
  Write-Output ('PASS selected '+$paths[$i])
 }
 $rect=New-Object DevNative+Rect
 $null=[DevNative]::GetWindowRect($dev,[ref]$rect)
 $bitmap=New-Object System.Drawing.Bitmap(($rect.Right-$rect.Left),($rect.Bottom-$rect.Top))
 $graphics=[System.Drawing.Graphics]::FromImage($bitmap)
 $dc=$graphics.GetHdc()
 try {$null=[DevNative]::PrintWindow($dev,$dc,2)} finally {$graphics.ReleaseHdc($dc)}
 $bitmap.Save((Join-Path $workspacePath 'artifacts\developer-window\inspector.png'))
 $graphics.Dispose();$bitmap.Dispose()
 Press-F12 $dev
 Start-Sleep -Milliseconds 500
 if([DevNative]::IsWindowVisible($dev)){throw 'F12 did not close inspector'}
 Press-F12 $main
 $reopened=Wait-Window 'F12 開発者ウィンドウ'
 if($reopened -ne $dev){throw 'Inspector unexpectedly duplicated'}
 Write-Output 'PASS F12 opens, closes and reopens the same developer window'
 $null=[DevNative]::PostMessageW($main,0x10,[IntPtr]::Zero,[IntPtr]::Zero)
 if(!$demo.WaitForExit(10000)){throw 'Demo did not exit with inspector open'}
 if($demo.ExitCode -ne 0){throw ('Demo exit code '+$demo.ExitCode)}
 Write-Output 'PASS demo shutdown with inspector open'
} finally {
 if(!$demo.HasExited){$demo.Kill();$demo.WaitForExit()}
}
