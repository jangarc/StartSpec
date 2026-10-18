#Requires -Version 7

<#
.SYNOPSIS
    Thin wrapper for `dotnet ef migrations add` in the Iam module (repo-independent invocation).

.DESCRIPTION
    Pins the layout-derived paths this repo requires (module single-project + Hosts.Migrator as the
    sole design-time startup), so callers need only a migration name. `-DryRun` prints the resolved
    command without executing it.
    固化本 repo 的 EF 遷移路徑（模組單專案＋Hosts.Migrator 為唯一 design-time 啟動面）；呼叫端僅需
    遷移名稱。`-DryRun` 只印出解析後的指令、不執行（驗證用）。

.EXAMPLE
    pwsh -NoProfile -File scripts/Add-IamMigration.ps1 -Name AddIamAuthorization

.EXAMPLE
    pwsh -NoProfile -File scripts/Add-IamMigration.ps1 -Name AddIamAuthorization -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]*$')]
    [string]$Name,

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'src\Modules\Iam\Iam.csproj'
$startupProject = Join-Path $repoRoot 'src\Hosts\Hosts.Migrator\Hosts.Migrator.csproj'
$outputDir = 'Infrastructure\Migrations'

if (-not (Test-Path -LiteralPath $project)) { throw "Iam project not found: $project" }
if (-not (Test-Path -LiteralPath $startupProject)) { throw "Hosts.Migrator project not found: $startupProject" }

if ($DryRun) {
    "dotnet ef migrations add $Name --project $project --startup-project $startupProject --output-dir $outputDir"
    return
}

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

dotnet ef migrations add $Name --project $project --startup-project $startupProject --output-dir $outputDir

exit $LASTEXITCODE
