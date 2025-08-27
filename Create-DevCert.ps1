param(
  [string]$Password = "TodoApp2024!",
  [string]$FileName = "todoapp-cert.pfx"
)

$certPath = Join-Path $env:USERPROFILE $FileName

Write-Host "Generating development HTTPS certificate at: $certPath"
# Export a PFX with the specified password and trust the dev cert locally (where supported)
dotnet dev-certs https -ep $certPath -p $Password | Out-Host
try {
  dotnet dev-certs https --trust | Out-Host
} catch {
  Write-Host "Trust step skipped or not supported on this OS."
}

if (Test-Path $certPath) {
  Write-Host "Certificate created successfully: $certPath"
} else {
  Write-Error "Certificate was not created."
  exit 1
}
