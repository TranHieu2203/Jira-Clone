# Regenerate Init*_Oracle for one module (destructive to Migrations folder during run — backup first).
# Usage: .\regenerate-oracle-migrations.ps1 -ModuleName Workflow -ContextName WorkflowDbContext
param(
  [Parameter(Mandatory = $true)][string]$ModuleName,
  [Parameter(Mandatory = $true)][string]$ContextName
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root

$infra = "src/Modules/$ModuleName/$ModuleName.Infrastructure"
$backup = Join-Path ([System.IO.Path]::GetTempPath()) "jira-oracle-gen-$ModuleName"
Remove-Item $backup -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item "$infra/Migrations" $backup -Recurse
Remove-Item "$infra/Migrations/*" -Recurse -Force

$migrationName = "Init${ModuleName}_Oracle"
dotnet ef migrations add $migrationName --project $infra --context $ContextName --startup-project src/Bootstrapper/Api.Host -- Oracle

New-Item -ItemType Directory -Path "$infra/Migrations/Oracle" -Force | Out-Null
Get-ChildItem "$infra/Migrations" -Filter "*_Oracle*.cs" | ForEach-Object { Move-Item $_.FullName "$infra/Migrations/Oracle/" }
Remove-Item "$infra/Migrations/*ModelSnapshot.cs" -Force -ErrorAction SilentlyContinue
Copy-Item "$backup\*" "$infra/Migrations" -Force

Write-Host "Done $ModuleName — verify build: dotnet build"
