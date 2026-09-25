<#
.SYNOPSIS
Formats JSON with compact leaf objects and arrays, wrapping at 80 columns.

.EXAMPLE
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Format-Json.ps1 App_Data/demo.stationery-style.json
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Path,

    [ValidateRange(40, 240)]
    [int] $Width = 80
)

$ErrorActionPreference = 'Stop'
$resolvedPath = (Resolve-Path -LiteralPath $Path).Path
$jsonText = [System.IO.File]::ReadAllText($resolvedPath)
$document = ConvertFrom-Json -InputObject $jsonText

function Remove-UnitSpans($Value) {
    if ($Value -is [System.Array]) {
        foreach ($item in $Value) { Remove-UnitSpans $item }
        return
    }
    if ($Value -isnot [System.Management.Automation.PSCustomObject]) { return }
    foreach ($property in @($Value.PSObject.Properties)) {
        Remove-UnitSpans $property.Value
        if ($property.Name -in @('colspan', 'rowspan') -and
            ($property.Value -is [int] -or $property.Value -is [long] -or $property.Value -is [double]) -and
            $property.Value -eq 1) {
            $Value.PSObject.Properties.Remove($property.Name)
        }
    }
}

Remove-UnitSpans $document

function Test-JsonContainer($Value) {
    return $null -ne $Value -and ($Value -is [System.Array] -or $Value -is [System.Management.Automation.PSCustomObject])
}

function ConvertTo-JsonScalar($Value) {
    return ConvertTo-Json -InputObject $Value -Compress -Depth 100
}

function ConvertTo-CompactLeaf($Value) {
    if ($Value -is [System.Array]) {
        $parts = @($Value | ForEach-Object { ConvertTo-JsonScalar $_ })
        return '[' + ($parts -join ', ') + ']'
    }
    $parts = @($Value.PSObject.Properties | ForEach-Object {
        (ConvertTo-JsonScalar $_.Name) + ': ' + (ConvertTo-JsonScalar $_.Value)
    })
    if ($parts.Count -eq 0) { return '{}' }
    return '{ ' + ($parts -join ', ') + ' }'
}

function Format-JsonValue($Value, [int] $Depth, [string] $Prefix) {
    $indent = ' ' * ($Depth * 4)
    $isArray = $Value -is [System.Array]

    if (-not (Test-JsonContainer $Value)) {
        return $Prefix + (ConvertTo-JsonScalar $Value)
    }

    if ($isArray) {
        $items = @($Value)
        if ($items.Count -eq 0) { return $Prefix + '[]' }
        $isLeaf = -not ($items | Where-Object { Test-JsonContainer $_ } | Select-Object -First 1)
        if ($isLeaf) {
            $compact = ConvertTo-CompactLeaf $Value
            if ($Prefix.TrimStart().Length + $compact.Length -le $Width) { return $Prefix + $compact }
        }

        $lines = [System.Collections.Generic.List[string]]::new()
        $lines.Add($Prefix + '[')
        for ($index = 0; $index -lt $items.Count; $index++) {
            $itemLines = @(Format-JsonValue $items[$index] ($Depth + 1) (' ' * (($Depth + 1) * 4)))
            if ($index -lt $items.Count - 1) {
                $itemLines[$itemLines.Count - 1] += ','
            }
            foreach ($line in $itemLines) { $lines.Add($line) }
        }
        $lines.Add($indent + ']')
        return $lines.ToArray()
    }

    $properties = @($Value.PSObject.Properties)
    if ($properties.Count -eq 0) { return $Prefix + '{}' }
    $isLeaf = -not ($properties | Where-Object { Test-JsonContainer $_.Value } | Select-Object -First 1)
    if ($isLeaf) {
        $compact = ConvertTo-CompactLeaf $Value
        if ($Prefix.TrimStart().Length + $compact.Length -le $Width) { return $Prefix + $compact }
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add($Prefix + '{')
    for ($index = 0; $index -lt $properties.Count; $index++) {
        $property = $properties[$index]
        $key = ConvertTo-JsonScalar $property.Name
        $propertyPrefix = (' ' * (($Depth + 1) * 4)) + $key + ': '
        $valueLines = @(Format-JsonValue $property.Value ($Depth + 1) $propertyPrefix)
        if ($index -lt $properties.Count - 1) {
            $valueLines[$valueLines.Count - 1] += ','
        }
        foreach ($line in $valueLines) { $lines.Add($line) }
    }
    $lines.Add($indent + '}')
    return $lines.ToArray()
}

$formatted = @(Format-JsonValue $document 0 '') -join [Environment]::NewLine
[System.IO.File]::WriteAllText($resolvedPath, $formatted + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
