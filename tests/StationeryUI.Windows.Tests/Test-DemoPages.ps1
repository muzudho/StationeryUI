param(
 [string[]]$Scenarios = @('page-hint','page-hint-clear','page-open','page-back','page-keyboard','split-vertical','split-horizontal','split-keyboard','layout-open','partial-switch'),
 [string[]]$Scales = @('1','1.5')
)
$ErrorActionPreference = 'Stop'
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$demoPath = Join-Path $workspacePath 'samples/StationeryUI.Demo/bin/Release/net8.0-windows/StationeryUI.Demo.exe'
$outputPath = Join-Path $workspacePath 'artifacts/page-smoke'
New-Item -ItemType Directory -Force $outputPath | Out-Null
$env:STATIONERYUI_CONFIG_PATH = ''
foreach ($scenario in $Scenarios) {
 foreach ($scale in $Scales) {
  $env:STATIONERYUI_SMOKE_CASE = $scenario
  $env:STATIONERYUI_SMOKE_SCALE = $scale
  $env:STATIONERYUI_SMOKE_THEME = if ($scale -eq '1') {'light'} else {'dark'}
  $env:STATIONERYUI_SMOKE_PNG = Join-Path $outputPath ($scenario + '-' + $scale + '.png')
  $process = Start-Process -FilePath $demoPath -WindowStyle Hidden -PassThru
  try {
   if (!$process.WaitForExit(20000)) { throw "Timeout: $scenario $scale" }
   if ($process.ExitCode -ne 0) { throw "Failure: $scenario $scale code=$($process.ExitCode)" }
   if (!(Test-Path -LiteralPath $env:STATIONERYUI_SMOKE_PNG)) { throw 'Screenshot missing' }
   Write-Output "PASS $scenario scale=$scale"
  } finally { if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() } }
 }
}

