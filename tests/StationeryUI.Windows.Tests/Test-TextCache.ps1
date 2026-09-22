$ErrorActionPreference = 'Stop'
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputPath = Join-Path $workspacePath 'artifacts/text-cache-test'
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$executablePath = Join-Path $PSScriptRoot 'bin/Release/net8.0-windows/StationeryUI.Windows.Tests.exe'
$stdoutPath = Join-Path $outputPath 'stdout.txt'
$stderrPath = Join-Path $outputPath 'stderr.txt'
$process = Start-Process -FilePath $executablePath -ArgumentList '--text-cache' -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
$null = $process.Handle
try {
    if (!$process.WaitForExit(30000)) { throw 'Text cache rendering test timed out.' }
    Get-Content -LiteralPath $stdoutPath
    Get-Content -LiteralPath $stderrPath
    if ($process.ExitCode -ne 0) { throw "Text cache rendering test failed: $($process.ExitCode)" }
} finally {
    if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
}
