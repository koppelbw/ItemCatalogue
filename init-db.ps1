#requires -Version 5.1
<#
.SYNOPSIS
    Bring up the local SQL Server container and publish the ItemCatalogue schema to it from the
    SSDT project (Database.dacpac) using SqlPackage.

.DESCRIPTION
    The SSDT project (Database/Database.sqlproj) is the schema source of truth — there are no EF
    migrations. This script starts the container defined in docker-compose.sqlserver.yml, waits for
    the engine, then publishes the dacpac (creating the database, tables, FKs/constraints and running
    the post-deployment seed scripts). Publishing is diff-based and idempotent, so it is safe to
    re-run any time to update or reset the schema.

.EXAMPLE
    ./init-db.ps1
    ./init-db.ps1 -Rebuild          # rebuild the dacpac from the .sqlproj first (needs MSBuild/SSDT)
#>
[CmdletBinding()]
param(
    [string]$SaPassword   = $(if ($env:MSSQL_SA_PASSWORD) { $env:MSSQL_SA_PASSWORD } else { 'LocalDev!Pass123' }),
    [string]$DatabaseName = 'ItemCatalogue',
    [int]   $Port         = 1433,
    [switch]$Rebuild
)

$ErrorActionPreference = 'Stop'
$root    = $PSScriptRoot
$compose = Join-Path $root 'docker-compose.sqlserver.yml'
$sqlproj = Join-Path $root 'Database\Database.sqlproj'
$dacpac  = Join-Path $root 'Database\bin\Debug\Database.dacpac'

# 1. Start the container (compose reads MSSQL_SA_PASSWORD from the environment).
Write-Host '==> Starting SQL Server container...' -ForegroundColor Cyan
$env:MSSQL_SA_PASSWORD = $SaPassword
docker compose -f $compose up -d
if ($LASTEXITCODE -ne 0) { throw 'docker compose up failed. Is Docker Desktop running?' }

# 2. Wait until the engine accepts connections (first start initialises the data volume).
Write-Host '==> Waiting for SQL Server to accept connections...' -ForegroundColor Cyan
$masterCs = "Server=localhost,$Port;Database=master;User Id=sa;Password=$SaPassword;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5"
$ready = $false
foreach ($attempt in 1..60) {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection $masterCs
        $conn.Open(); $conn.Close()
        $ready = $true; break
    } catch {
        Start-Sleep -Seconds 2
    }
}
if (-not $ready) { throw 'SQL Server did not become ready within ~2 minutes.' }
Write-Host '    ready.' -ForegroundColor Green

# 3. Optionally rebuild the dacpac from the SSDT project (.sqlproj is non-SDK style -> needs MSBuild).
if ($Rebuild) {
    Write-Host '==> Building Database.sqlproj...' -ForegroundColor Cyan
    $msbuild = (Get-Command msbuild -ErrorAction SilentlyContinue).Source
    if (-not $msbuild) {
        throw 'MSBuild not found. Run from a "Developer PowerShell for VS", or build the Database project in Visual Studio, then re-run without -Rebuild.'
    }
    & $msbuild $sqlproj /p:Configuration=Debug /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw 'Database.sqlproj build failed.' }
}
if (-not (Test-Path $dacpac)) {
    throw "Dacpac not found at $dacpac. Build the Database project in Visual Studio, or re-run with -Rebuild."
}

# 4. Ensure the SqlPackage CLI is available.
if (-not (Get-Command sqlpackage -ErrorAction SilentlyContinue)) {
    Write-Host '==> Installing SqlPackage (dotnet global tool)...' -ForegroundColor Cyan
    dotnet tool install -g microsoft.sqlpackage
    if ($LASTEXITCODE -ne 0) { throw 'Failed to install SqlPackage.' }
    $env:PATH += ";$HOME\.dotnet\tools"
}

# 5. Publish: creates the DB + tables + FKs and runs the post-deployment seed scripts. Idempotent.
Write-Host "==> Publishing schema to database '$DatabaseName'..." -ForegroundColor Cyan
sqlpackage /Action:Publish `
    /SourceFile:"$dacpac" `
    /TargetServerName:"localhost,$Port" `
    /TargetDatabaseName:"$DatabaseName" `
    /TargetUser:"sa" `
    /TargetPassword:"$SaPassword" `
    /TargetEncryptConnection:True `
    /TargetTrustServerCertificate:True `
    /p:AllowIncompatiblePlatform=True   # dacpac targets SQL 2025 (Sql170); container is 2022. Schema uses no 2025-only features.
if ($LASTEXITCODE -ne 0) { throw 'SqlPackage publish failed.' }

Write-Host ''
Write-Host "Done. SQL Server is running on localhost,$Port and database '$DatabaseName' is provisioned." -ForegroundColor Green
Write-Host 'The API will use it automatically (see appsettings.Development.json -> ConnectionStrings:local).' -ForegroundColor Green
