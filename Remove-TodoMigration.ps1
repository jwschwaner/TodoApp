# PowerShell script to remove the most recent migration from the Todo context
Write-Host "Removing the latest migration from Todo database..." -ForegroundColor Cyan
dotnet ef migrations remove --project TodoApp --context TodoDbContext

Write-Host "Migration removed successfully." -ForegroundColor Green
