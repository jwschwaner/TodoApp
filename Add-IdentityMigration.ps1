# PowerShell script to add a new migration to the Identity context
param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$MigrationName,
    [Parameter(Mandatory=$false, Position=1)]
    [string]$Project = 'TodoApp'
)

Write-Host "Adding new migration '$MigrationName' to Identity database in project '$Project'..." -ForegroundColor Cyan

dotnet ef migrations add $MigrationName --project $Project --context ApplicationDbContext --output-dir Data/Migrations

Write-Host "Migration added successfully to $Project/Data/Migrations." -ForegroundColor Green
Write-Host "Run Update-databases.ps1 to apply the migration (for the Kestrel project)." -ForegroundColor Yellow
