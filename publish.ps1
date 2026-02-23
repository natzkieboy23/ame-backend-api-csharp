# publish.ps1
# Publishes InventoryApi as a self-contained single-file Windows executable.
# Output goes to: backend-api-csharp\publish\
#
# Run this BEFORE building the Inno Setup installer.
# Usage (from backend-api-csharp\ folder):
#   .\publish.ps1

Set-Location $PSScriptRoot

$project    = Join-Path $PSScriptRoot "InventoryApi"
$outputPath = Join-Path $PSScriptRoot "publish"

Write-Host ""
Write-Host "  AME Inventory — Publishing self-contained release..." -ForegroundColor Cyan
Write-Host ""

dotnet publish "$project" `
    -c Release `
    -r win-x86 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:SyncBuild=false `
    -o "$outputPath"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  Publish FAILED." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "  Published successfully to: $outputPath" -ForegroundColor Green
Write-Host ""
Write-Host "  Files ready for installer:" -ForegroundColor Yellow
Get-ChildItem $outputPath | Format-Table Name, @{N="Size";E={"{0:N0} KB" -f ($_.Length/1KB)}} -AutoSize
Write-Host ""
Write-Host "  Next step: open installer.iss in Inno Setup and click Build > Compile." -ForegroundColor Cyan
