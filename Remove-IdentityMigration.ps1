# PowerShell script to remove the most recent migration from the Identity context
Write-Host "Removing the latest migration from Identity database..." -ForegroundColor Cyan
dotnet ef migrations remove --project TodoApp --context ApplicationDbContext

Write-Host "Migration removed successfully." -ForegroundColor Green
