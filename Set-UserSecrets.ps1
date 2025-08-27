param(
  [string]$ProjectDir = "TodoApp/TodoApp",
  [string]$CertFileName = "todoapp-cert.pfx",
  [string]$Password = "TodoApp2024!"
)

$fullProjectPath = Resolve-Path $ProjectDir
$certPath = Join-Path $env:USERPROFILE $CertFileName

Write-Host "Setting user-secrets for project: $fullProjectPath"
Write-Host "Using certificate path: $certPath"

Push-Location $fullProjectPath
try {
  dotnet user-secrets init | Out-Host
  dotnet user-secrets set "Kestrel:Certificates:Default:Path" $certPath | Out-Host
  dotnet user-secrets set "Kestrel:Certificates:Default:Password" $Password | Out-Host
  dotnet user-secrets list | Out-Host
} finally {
  Pop-Location
}

Write-Host "User-secrets configured."
