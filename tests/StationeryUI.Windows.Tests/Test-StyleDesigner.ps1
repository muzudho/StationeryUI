param([string]$Executable = 'samples/StationeryUI.StyleDesigner/bin/Release/net8.0-windows/StationeryUI.StyleDesigner.exe', [switch]$Existing, [switch]$NativeDialog, [switch]$CancelDialog, [switch]$Dark, [switch]$SavePoints, [ValidateSet('panel','floating','delete','rename')][string]$LayoutEditing)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testDirectory = Join-Path $workspace ('artifacts/style-designer-test/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$previousOutput = $env:STATIONERYUI_DESIGNER_TEST_OUTPUT
$previousInput = $env:STATIONERYUI_DESIGNER_TEST_INPUT
$previousNative = $env:STATIONERYUI_DESIGNER_TEST_NATIVE_DIALOG
$previousCancel = $env:STATIONERYUI_DESIGNER_TEST_CANCEL_DIALOG
$previousDark = $env:STATIONERYUI_DESIGNER_TEST_DARK
$previousLayout = $env:STATIONERYUI_DESIGNER_TEST_LAYOUT
$previousSavePoints = $env:STATIONERYUI_DESIGNER_TEST_SAVEPOINTS
$process = $null
try {
    if ($SavePoints) { $Existing = $true }
    $env:STATIONERYUI_DESIGNER_TEST_SAVEPOINTS = if ($SavePoints) { '1' } else { '' }
    $env:STATIONERYUI_DESIGNER_TEST_OUTPUT = $testDirectory
    $env:STATIONERYUI_DESIGNER_TEST_INPUT = ''
    $env:STATIONERYUI_DESIGNER_TEST_LAYOUT = $LayoutEditing
    $env:STATIONERYUI_DESIGNER_TEST_NATIVE_DIALOG = if ($NativeDialog) { '1' } else { '' }
    $env:STATIONERYUI_DESIGNER_TEST_CANCEL_DIALOG = if ($CancelDialog) { '1' } else { '' }
    $env:STATIONERYUI_DESIGNER_TEST_DARK = if ($Dark) { '1' } else { '' }
    if ($Existing) {
        $source = Join-Path $testDirectory 'demo.stationery-style.json'
        Copy-Item -LiteralPath (Join-Path $workspace 'App_Data/demo.stationery-style.json') -Destination $source
        $sourceHash = (Get-FileHash -LiteralPath $source).Hash
        $env:STATIONERYUI_DESIGNER_TEST_INPUT = $source
    }
    $process = Start-Process -FilePath (Join-Path $workspace $Executable) -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(30000)) { throw 'Style designer test timed out.' }
    if ($process.ExitCode -ne 0) { throw "Style designer failed: $($process.ExitCode). Check $testDirectory/error.txt" }
    if ($CancelDialog) {
        if (Test-Path -LiteralPath (Join-Path $testDirectory 'plan.stationery-style.json')) { throw 'Cancel exported a file.' }
        Write-Output "PASS native dialog cancellation stays on first page: $testDirectory"
        return
    }
    $json = Get-Content -LiteralPath (Join-Path $testDirectory 'plan.stationery-style.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($SavePoints) {
        if ((Get-FileHash -LiteralPath $source).Hash -ne $sourceHash) { throw 'Restore did not recover original bytes.' }
        if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'savepoints.png'))) { throw 'Savepoint picker screenshot missing.' }
    } elseif ($LayoutEditing) {
        if ($json.layouts.Count -ne $(if ($LayoutEditing -eq 'delete') { 1 } else { 2 })) { throw 'Layout count mismatch.' }
    } elseif ($Existing) {
        $layout = $json.layouts | Where-Object id -eq 'topDemoLayout'
        if ($layout.'column-definitions'[0] -ne '2.5rate') { throw 'Imported edit missing.' }
        if ((Get-FileHash -LiteralPath "$source.1.bak").Hash -ne $sourceHash) { throw 'Savepoint differs from original.' }
        $saved = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json
        if (($saved.layouts | Where-Object id -eq 'topDemoLayout').'column-definitions'[0] -ne '2.5rate') { throw 'Autosave missing.' }
        $original = Get-Content -LiteralPath "$source.1.bak" -Raw -Encoding UTF8 | ConvertFrom-Json
        if (($original.models | ConvertTo-Json -Depth 100 -Compress) -ne ($json.models | ConvertTo-Json -Depth 100 -Compress)) { throw 'Models changed.' }
        if (($original.bindings | ConvertTo-Json -Depth 100 -Compress) -ne ($json.bindings | ConvertTo-Json -Depth 100 -Compress)) { throw 'Bindings changed.' }
    } else {
        if ($json.layouts[0].'column-definitions'[0] -ne '1.5rate' -or $json.layouts[0].'column-definitions'[1] -ne '120px') { throw 'Track export mismatch.' }
        if ($json.models[0].children[0].children[0].type -ne 'container') { throw 'Layout-only editor changed model type.' }
    }
    if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'welcome.png'))) { throw 'Welcome screenshot missing.' }
    if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'designer.png'))) { throw 'Screenshot missing.' }
    Write-Output "PASS designer cell editing, mixed units, JSON export and screenshot: $testDirectory"
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $env:STATIONERYUI_DESIGNER_TEST_OUTPUT = $previousOutput
    $env:STATIONERYUI_DESIGNER_TEST_INPUT = $previousInput
    $env:STATIONERYUI_DESIGNER_TEST_NATIVE_DIALOG = $previousNative
    $env:STATIONERYUI_DESIGNER_TEST_CANCEL_DIALOG = $previousCancel
    $env:STATIONERYUI_DESIGNER_TEST_DARK = $previousDark
    $env:STATIONERYUI_DESIGNER_TEST_LAYOUT = $previousLayout
    $env:STATIONERYUI_DESIGNER_TEST_SAVEPOINTS = $previousSavePoints
}
