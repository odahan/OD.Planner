[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'src\OD.Planner\OD.Planner.csproj'
$publishDirectory = Join-Path $PSScriptRoot 'src\OD.Planner\bin\publish'
$publishExecutable = Join-Path $publishDirectory 'OD.Planner.exe'

# A single-file publish must replace the existing executable.
if (Test-Path -LiteralPath $publishExecutable) {
    $fileHandle = $null
    try {
        $fileHandle = [System.IO.File]::Open(
            $publishExecutable,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
    }
    catch [System.IO.IOException], [System.UnauthorizedAccessException] {
        throw "Cannot replace '$publishExecutable'. Close OD.Planner and any application using this file, then run the script again."
    }
    finally {
        if ($null -ne $fileHandle) {
            $fileHandle.Dispose()
        }
    }
}

& dotnet publish $projectPath --configuration Release --property:PublishProfile=FolderProfile -m:1

if ($LASTEXITCODE -ne 0) {
    throw "Publishing failed with exit code $LASTEXITCODE."
}

Write-Host "Release published to: $publishDirectory"
