param([string]$Executable = 'samples/StationeryUIEditor/bin/Release/net8.0-windows/StationeryUIEditor.exe', [switch]$Existing, [switch]$NativeDialog, [switch]$CancelDialog, [switch]$Dark, [switch]$SavePoints, [ValidateSet('panel','floating','delete','rename')][string]$LayoutEditing)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testDirectory = Join-Path $workspace ('artifacts/ui-editor-test/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$previousOutput = $env:STATIONERYUI_EDITOR_TEST_OUTPUT
$previousInput = $env:STATIONERYUI_EDITOR_TEST_INPUT
$previousNative = $env:STATIONERYUI_EDITOR_TEST_NATIVE_DIALOG
$previousCancel = $env:STATIONERYUI_EDITOR_TEST_CANCEL_DIALOG
$previousDark = $env:STATIONERYUI_EDITOR_TEST_DARK
$previousLayout = $env:STATIONERYUI_EDITOR_TEST_LAYOUT
$previousSavePoints = $env:STATIONERYUI_EDITOR_TEST_SAVEPOINTS
$process = $null
try {
    if ($SavePoints) { $Existing = $true }
    $env:STATIONERYUI_EDITOR_TEST_SAVEPOINTS = if ($SavePoints) { '1' } else { '' }
    $env:STATIONERYUI_EDITOR_TEST_OUTPUT = $testDirectory
    $env:STATIONERYUI_EDITOR_TEST_INPUT = ''
    $env:STATIONERYUI_EDITOR_TEST_LAYOUT = $LayoutEditing
    $env:STATIONERYUI_EDITOR_TEST_NATIVE_DIALOG = if ($NativeDialog) { '1' } else { '' }
    $env:STATIONERYUI_EDITOR_TEST_CANCEL_DIALOG = if ($CancelDialog) { '1' } else { '' }
    $env:STATIONERYUI_EDITOR_TEST_DARK = if ($Dark) { '1' } else { '' }
    if ($Existing) {
        $source = Join-Path $testDirectory 'demo.stationery-ui.json'
        Copy-Item -LiteralPath (Join-Path $workspace 'App_Data/demo.stationery-ui.json') -Destination $source
        $sourceHash = (Get-FileHash -LiteralPath $source).Hash
        $env:STATIONERYUI_EDITOR_TEST_INPUT = $source
    }
    $process = Start-Process -FilePath (Join-Path $workspace $Executable) -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(30000)) { throw 'UI editor test timed out.' }
    if ($process.ExitCode -ne 0) { throw "UI editor failed: $($process.ExitCode). Check $testDirectory/error.txt" }
    if ($CancelDialog) {
        if (Test-Path -LiteralPath (Join-Path $testDirectory 'plan.stationery-ui.json')) { throw 'Cancel exported a file.' }
        Write-Output "PASS native dialog cancellation stays on first page: $testDirectory"
        return
    }
    $json = Get-Content -LiteralPath (Join-Path $testDirectory 'plan.stationery-ui.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($SavePoints) {
        if ((Get-FileHash -LiteralPath $source).Hash -ne $sourceHash) { throw 'Restore did not recover original bytes.' }
        if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'savepoints.png'))) { throw 'Savepoint picker screenshot missing.' }
    } elseif ($LayoutEditing) {
        if ($json.layouts.Count -ne $(if ($LayoutEditing -eq 'delete') { 1 } else { 2 })) { throw 'Layout count mismatch.' }
    } elseif ($Existing) {
        $layout = $json.viewports[0].children[0].children[0].layout
        if ($layout.'column-definitions'[0] -ne '2.5rate') { throw 'Imported edit missing.' }
        if ((Get-FileHash -LiteralPath "$source.1.bak").Hash -ne $sourceHash) { throw 'Savepoint differs from original.' }
        $saved = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($saved.viewports[0].children[0].children[0].layout.'column-definitions'[0] -ne '2.5rate') { throw 'Autosave missing.' }
        $original = Get-Content -LiteralPath "$source.1.bak" -Raw -Encoding UTF8 | ConvertFrom-Json
        $original.viewports[0].children[0].children[0].layout.'column-definitions'[0] = '2.5rate'
        if (($original | ConvertTo-Json -Depth 100 -Compress) -ne ($json | ConvertTo-Json -Depth 100 -Compress)) { throw 'Unrelated document content changed.' }
    } else {
        $grid = $json.layouts | Where-Object type -eq 'grid-layout' | Select-Object -First 1
        if ($grid.'column-definitions'[0] -ne '1.5rate' -or $grid.'column-definitions'[1] -ne '120px') { throw 'Track export mismatch.' }
        if ($json.modelTree.children[0].children[0].type -ne 'container') { throw 'Layout-only editor changed model type.' }
    }
    if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'welcome.png'))) { throw 'Welcome screenshot missing.' }
    if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'editor.png'))) { throw 'Screenshot missing.' }
    Write-Output "PASS UI editor cell editing, mixed units, JSON export and screenshot: $testDirectory"
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $env:STATIONERYUI_EDITOR_TEST_OUTPUT = $previousOutput
    $env:STATIONERYUI_EDITOR_TEST_INPUT = $previousInput
    $env:STATIONERYUI_EDITOR_TEST_NATIVE_DIALOG = $previousNative
    $env:STATIONERYUI_EDITOR_TEST_CANCEL_DIALOG = $previousCancel
    $env:STATIONERYUI_EDITOR_TEST_DARK = $previousDark
    $env:STATIONERYUI_EDITOR_TEST_LAYOUT = $previousLayout
    $env:STATIONERYUI_EDITOR_TEST_SAVEPOINTS = $previousSavePoints
}
