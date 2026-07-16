param(
    # Auto-detects the exact name of the parent folder
    [string] $ModName = (Split-Path (Split-Path $PSScriptRoot -Parent) -Leaf),
    
    # Points to your Unity project (up three levels to 'repos', then into 'timberborn-modding-main')
    [string] $UnityProjectPath = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\..\timberborn-modding-main"),
    
    # Points to your master script (up two levels to 'Mods', then into 'tools')
    [string] $CentralScriptPath = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\tools\Build-AssetBundles.ps1"),

    # Auto-detects version from the current folder leaf (e.g., 'Version-1.0' -> '1.0')
    [string] $CompatibilityVersion = ((Split-Path $PSScriptRoot -Leaf) -replace '^Version-', '')
)

$currentFolder = $PSScriptRoot

Write-Host "======================================================="
# FIXED: Wrapped in $() so the colon doesn't break the parser
Write-Host "$($ModName): UNITY ASSET BUNDLE EXPORT STARTING"
Write-Host "======================================================="

# Compile AssetBundles via Central Tool
& $CentralScriptPath `
    -ModName $ModName `
    -UnityProjectPath $UnityProjectPath `
    -OutputFolder $currentFolder `
    -CompatibilityVersion $CompatibilityVersion

Write-Host "======================================================="
Write-Host " ASSET EXPORT COMPLETE." -ForegroundColor Green
pause