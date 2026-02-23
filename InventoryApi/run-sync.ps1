# run-sync.ps1
# Builds the sync console to a separate output folder (bin\sync\) so it never
# conflicts with the running web API exe, then launches it.
#
# Usage (from the InventoryApi folder):
#   .\run-sync.ps1

Set-Location $PSScriptRoot
dotnet run -p:SyncBuild=true -- --sync
