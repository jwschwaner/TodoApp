# PowerShell script to add a new migration to the Identity context
param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$MigrationName
)

Write-Host "Adding new migration '$MigrationName' to Identity database..." -ForegroundColor Cyan
dotnet ef migrations add $MigrationName --project TodoApp --context ApplicationDbContext

Write-Host "Migration added successfully." -ForegroundColor Green
Write-Host "Run Update-databases.ps1 to apply the migration." -ForegroundColor Yellow
