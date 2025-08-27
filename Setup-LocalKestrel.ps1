param(
  [string]$CertPassword = "TodoApp2024!",
  [string]$CertFileName = "todoapp-cert.pfx",
  [string]$ProjectDir = "TodoApp"
)

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $here
try {
  Write-Host "[1/2] Creating dev certificate in user profile..."
  ./Create-DevCert.ps1 -Password $CertPassword -FileName $CertFileName

  Write-Host "[2/2] Setting user-secrets for Kestrel..."
  ./Set-UserSecrets.ps1 -ProjectDir $ProjectDir -CertFileName $CertFileName -Password $CertPassword

  Write-Host "Local Kestrel setup complete. You can now run:"
  Write-Host "  cd TodoApp; dotnet run"
}
finally {
  Pop-Location
}
