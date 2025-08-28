param(
  [string]$ProjectDir = "TodoApp", # corrected default to the actual project folder
  [string]$CertFileName = "todoapp-cert.pfx",
  [string]$Password = "TodoApp2024!"
)

# Resolve project directory and csproj
try {
  $fullProjectPath = Resolve-Path -LiteralPath $ProjectDir -ErrorAction Stop
} catch {
  Write-Error "Project directory '$ProjectDir' not found from '$((Get-Location).Path)'."; throw
}

$csproj = Get-ChildItem -Path $fullProjectPath -Filter *.csproj -File -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $csproj) { Write-Error "No .csproj found under '$fullProjectPath'."; throw }
$csprojPath = $csproj.FullName

$certPath = Join-Path $env:USERPROFILE $CertFileName

Write-Host "Setting user-secrets for project: $csprojPath"
Write-Host "Using certificate path: $certPath"

# Initialize and set secrets explicitly for the project
$null = dotnet user-secrets init --project "$csprojPath" | Out-Host
$null = dotnet user-secrets set --project "$csprojPath" "Kestrel:Certificates:Default:Path" "$certPath" | Out-Host
$null = dotnet user-secrets set --project "$csprojPath" "Kestrel:Certificates:Default:Password" "$Password" | Out-Host

# Ensure RSA encryption keys exist for stable envelope decryption across restarts
$secrets = dotnet user-secrets list --project "$csprojPath"
$hasPriv = $secrets -match "ENCRYPTION__RSA__PRIVATEKEY_PKCS8_BASE64"
$hasPub  = $secrets -match "ENCRYPTION__RSA__PUBLICKEY_SPKI_BASE64"

function New-RsaKeysViaDotnet {
  param([int]$KeySize = 2048)
  # Generates Base64-encoded PKCS#8 private key and SPKI public key using a temporary .NET console app
  $tmp = Join-Path $env:TEMP ("todoapp-keygen-" + [guid]::NewGuid().ToString("N"))
  New-Item -ItemType Directory -Path $tmp | Out-Null
  $proj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
"@
  $prog = @"
using System;
using System.Security.Cryptography;

class Program {
  static int Main(string[] args) {
    int keySize = 2048;
    if (args.Length > 0 && int.TryParse(args[0], out var k) && k >= 1024) keySize = k;
    using var rsa = RSA.Create(keySize);
    var priv = rsa.ExportPkcs8PrivateKey();
    var pub  = rsa.ExportSubjectPublicKeyInfo();
    Console.WriteLine(Convert.ToBase64String(priv));
    Console.WriteLine(Convert.ToBase64String(pub));
    return 0;
  }
}
"@
  Set-Content -LiteralPath (Join-Path $tmp "KeyGen.csproj") -Value $proj -Encoding UTF8
  Set-Content -LiteralPath (Join-Path $tmp "Program.cs") -Value $prog -Encoding UTF8
  $build = & dotnet build "$tmp" -c Release -nologo 2>&1
  if ($LASTEXITCODE -ne 0) { throw "Failed to build temporary keygen: $build" }
  $runOut = & dotnet run --project "$tmp" -c Release --no-build -- $KeySize 2>&1
  if ($LASTEXITCODE -ne 0) { throw "Failed to run temporary keygen: $runOut" }
  $lines = $runOut -split "\r?\n" | Where-Object { $_ -and ($_ -notmatch "^\s*Build succeeded") -and ($_ -notmatch ": warning ") -and ($_ -notmatch ": error ") }
  # pick last two non-empty lines (priv, pub)
  $nonEmpty = @(); foreach ($l in $lines) { if ($l.Trim().Length -gt 0) { $nonEmpty += $l.Trim() } }
  if ($nonEmpty.Count -lt 2) { throw "Unexpected keygen output." }
  return @{ PrivateB64 = $nonEmpty[$nonEmpty.Count-2]; PublicB64 = $nonEmpty[$nonEmpty.Count-1] }
}

if (-not ($hasPriv -and $hasPub)) {
  Write-Host "Generating RSA keypair for encryption (PKCS#8 private, SPKI public)..."
  $privB64 = $null
  $pubB64  = $null
  try {
    # Try native APIs (available in PowerShell 7+ / .NET Core)
    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    try {
      $priv = $rsa.ExportPkcs8PrivateKey()
      $pub  = $rsa.ExportSubjectPublicKeyInfo()
      $privB64 = [Convert]::ToBase64String($priv)
      $pubB64  = [Convert]::ToBase64String($pub)
    } finally {
      $rsa.Dispose()
    }
  } catch {
    # Fallback for Windows PowerShell 5.1 where ExportPkcs8PrivateKey may be missing
    $keys = New-RsaKeysViaDotnet -KeySize 2048
    $privB64 = $keys.PrivateB64
    $pubB64  = $keys.PublicB64
  }

  dotnet user-secrets set --project "$csprojPath" "ENCRYPTION__RSA__PRIVATEKEY_PKCS8_BASE64" "$privB64" | Out-Host
  dotnet user-secrets set --project "$csprojPath" "ENCRYPTION__RSA__PUBLICKEY_SPKI_BASE64" "$pubB64" | Out-Host
  Write-Host "Stored RSA keys in user-secrets."
} else {
  Write-Host "RSA encryption keys already present in user-secrets; skipping generation."
}

$null = dotnet user-secrets list --project "$csprojPath" | Out-Host

Write-Host "User-secrets configured."
