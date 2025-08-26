# PowerShell script to update both database contexts (Identity and Todo)
Write-Host "Updating Identity database..." -ForegroundColor Cyan
dotnet ef database update --project TodoApp --context ApplicationDbContext

Write-Host "Updating Todo database..." -ForegroundColor Cyan
dotnet ef database update --project TodoApp --context TodoDbContext

Write-Host "Database updates completed." -ForegroundColor Green
