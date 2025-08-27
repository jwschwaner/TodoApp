<#!
Tests the HTTPS-related assignment requirements for both the original Kestrel app and the Docker copy.

What it checks
- Cert file exists in the user folder and is loadable; prints trust verdict
- User-secrets contain Kestrel cert path/password (non-Docker project)
- Kestrel app responds on HTTPS and redirects HTTP->HTTPS
- Docker app responds on HTTPS and redirects HTTP->HTTPS
- Verifies remote cert thumbprints match the local PFX (for both Kestrel and Docker)

Usage (from repo root)
  # Full test (Kestrel + Docker)
  .\Test-HttpsRequirements.ps1

  # Skip either part
  .\Test-HttpsRequirements.ps1 -SkipKestrel
  .\Test-HttpsRequirements.ps1 -SkipDocker

Exit code is non-zero if any required check fails.
!#>
param(
  [switch]$SkipKestrel,
  [switch]$SkipDocker,
  [string]$CertPath = "$env:USERPROFILE\todoapp-cert.pfx",
  [string]$CertPassword = "TodoApp2024!",
  [int]$KestrelHttpPort = 5204,
  [int]$KestrelHttpsPort = 7181,
  [int]$DockerHttpPort = 5002,
  [int]$DockerHttpsPort = 5003,
  [int]$StartTimeoutSec = 20
)

$ErrorActionPreference = 'Stop'
$failed = @()

function Write-Heading($text) {
  Write-Host "`n=== $text ===" -ForegroundColor Cyan
}

function Test-CertFile {
  param([string]$Path,[string]$Password)
  Write-Heading "Checking cert file in user folder"
  if (-not (Test-Path $Path)) {
    Write-Host "Missing: $Path" -ForegroundColor Red
    return $false
  }
  try {
    $pfx = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $Path, $Password
    Write-Host ("Loaded PFX. Subject: {0}" -f $pfx.Subject)
    # Trust verdict (Windows store)
    $chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
    $trusted = $chain.Build($pfx)
    Write-Host ("Trusted in OS store: {0}" -f $trusted)
    return $true
  } catch {
    Write-Host "Failed to load PFX: $($_.Exception.Message)" -ForegroundColor Red
    return $false
  }
}

function Test-UserSecrets {
  param([string]$ProjectDir)
  Write-Heading "Checking user-secrets for Kestrel cert path/password"
  Push-Location $ProjectDir
  try {
    $out = dotnet user-secrets list 2>$null
    if (-not $out) {
      Write-Host "No secrets found." -ForegroundColor Yellow
      return $false
    }
    $hasPath = [bool]($out -match 'Kestrel:Certificates:Default:Path')
    $hasPass = [bool]($out -match 'Kestrel:Certificates:Default:Password')
    Write-Host "Has Path secret: $hasPath"
    Write-Host "Has Password secret: $hasPass"
    return ($hasPath -and $hasPass)
  } finally {
    Pop-Location
  }
}

function Wait-For-TlsReady {
  param([string]$TlsHost,[int]$Port,[int]$TimeoutSec=30)
  $sw = [Diagnostics.Stopwatch]::StartNew()
  while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
    try {
      $tcp = New-Object System.Net.Sockets.TcpClient($TlsHost,$Port)
      try {
        $ssl = New-Object System.Net.Security.SslStream($tcp.GetStream(), $false, ({ $true }))
        $ssl.AuthenticateAsClient($TlsHost)
        if ($ssl.IsAuthenticated) { return $true }
      } finally { if ($ssl) { $ssl.Dispose() }; $tcp.Dispose() }
    } catch { }
    Start-Sleep -Milliseconds 500
  }
  return $false
}

function Test-Redirect {
  param([string]$FromUrl,[string]$ExpectedPrefix)
  $resp = $null
  try {
    $resp = Invoke-WebRequest $FromUrl -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
  } catch {
    if ($_.Exception.Response) { $resp = $_.Exception.Response }
  }
  if (-not $resp) {
    Write-Host "No response from $FromUrl" -ForegroundColor Yellow
    return $false
  }
  $code = [int]$resp.StatusCode
  $loc  = $resp.Headers['Location']
  Write-Host "Redirect check -> Status: $code, Location: $loc"
  return ($code -ge 300 -and $code -lt 400 -and $loc -like "$ExpectedPrefix*")
}

function Get-RemoteCert {
  param([string]$TlsHost,[int]$Port)
  $tcp = New-Object System.Net.Sockets.TcpClient($TlsHost,$Port)
  try {
    $ssl = New-Object System.Net.Security.SslStream($tcp.GetStream(), $false, ({ $true }))
    $ssl.AuthenticateAsClient($TlsHost)
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $ssl.RemoteCertificate
    return $cert
  } finally {
    if ($ssl) { $ssl.Dispose() }
    $tcp.Dispose()
  }
}

function Test-CertMatchAndTrust {
  param([string]$LocalPfx,[string]$Password,[string]$TlsHost,[int]$Port)
  $local = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $LocalPfx, $Password
  $remote = Get-RemoteCert -TlsHost $TlsHost -Port $Port
  $match = ($local.Thumbprint -eq $remote.Thumbprint)
  $chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
  $trusted = $chain.Build($remote)
  Write-Host "Local Thumb:  $($local.Thumbprint)"
  Write-Host "Remote Thumb: $($remote.Thumbprint)"
  Write-Host "Thumbprints match: $match"
  Write-Host "Remote cert trusted: $trusted"
  return ($match -and $trusted)
}

function Start-DockerPostgres {
  Write-Heading "Ensuring Docker Postgres is running (port 5433) for Kestrel tests"
  Push-Location Docker
  try {
    docker compose up -d postgres | Out-Null
  } finally {
    Pop-Location
  }
  # Wait for 5433 to accept TCP
  $deadline = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $deadline) {
    if ((Test-NetConnection localhost -Port 5433).TcpTestSucceeded) { return $true }
    Start-Sleep -Milliseconds 250
  }
  Write-Host "Postgres on :5433 did not become ready in time" -ForegroundColor Yellow
  return $false
}

function Start-KestrelApp {
  param([string]$ProjectDir)
  Write-Heading "Starting Kestrel app"
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = "dotnet"
  $psi.Arguments = "run"
  $psi.WorkingDirectory = $ProjectDir
  $psi.RedirectStandardOutput = $true
  $psi.RedirectStandardError = $true
  $psi.UseShellExecute = $false
  $psi.CreateNoWindow = $true
  # Ensure Dev and point to the Docker Postgres on 5433
  $psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development"
  $psi.Environment["ConnectionStrings__IdentityConnection"] = "Host=localhost;Port=5433;Database=Identity;Username=postgres;Password=postgres"
  $psi.Environment["ConnectionStrings__TodoConnection"]     = "Host=localhost;Port=5433;Database=Todo;Username=postgres;Password=postgres"
  $proc = [System.Diagnostics.Process]::Start($psi)
  return $proc
}

function Stop-ProcessSafe {
  param([System.Diagnostics.Process]$Proc)
  if ($null -ne $Proc -and -not $Proc.HasExited) {
    try { $Proc.Kill(); $Proc.WaitForExit(5000) | Out-Null } catch {}
  }
}

function Resolve-ProjectDir {
  # Try common locations
  $candidates = @(
    (Join-Path $PSScriptRoot 'TodoApp')
    (Join-Path $PSScriptRoot 'TodoApp\TodoApp')
  )
  foreach ($c in $candidates) {
    if (Test-Path (Join-Path $c 'TodoApp.csproj')) { return $c }
  }
  return $null
}

function Test-IISNegative {
  param([string]$ProjectDir,[int]$TimeoutSec=10)
  Write-Heading "IIS negative test (should fail fast)"
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName  = 'dotnet'
  $psi.Arguments = 'run'
  $psi.WorkingDirectory = $ProjectDir
  $psi.RedirectStandardOutput = $true
  $psi.RedirectStandardError  = $true
  $psi.UseShellExecute = $false
  $psi.CreateNoWindow = $true
  # Simulate IIS hosting
  $psi.Environment['IIS_SITE_NAME'] = 'TestSite'
  $psi.Environment['ASPNETCORE_IIS_HTTPAUTH'] = '1'
  # Keep same DB override to avoid unrelated failures
  $psi.Environment['ASPNETCORE_ENVIRONMENT'] = 'Development'
  $psi.Environment['ConnectionStrings__IdentityConnection'] = 'Host=localhost;Port=5433;Database=Identity;Username=postgres;Password=postgres'
  $psi.Environment['ConnectionStrings__TodoConnection']     = 'Host=localhost;Port=5433;Database=Todo;Username=postgres;Password=postgres'

  $proc = [System.Diagnostics.Process]::Start($psi)
  try {
    if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
      try { $proc.Kill() } catch {}
      Write-Host 'IIS negative test timed out.' -ForegroundColor Red
      return $false
    }
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $combined = "$stdout`n$stderr"
    $failedAsExpected = ($proc.ExitCode -ne 0) -and ($combined -match 'locked to the cross-platform Kestrel web server')
    Write-Host ("ExitCode: {0}" -f $proc.ExitCode)
    if (-not $failedAsExpected) {
      Write-Host 'IIS negative test output (tail):' -ForegroundColor Yellow
      ($combined -split "`r?`n") | Select-Object -Last 30 | ForEach-Object { Write-Host $_ }
    }
    return $failedAsExpected
  } finally {
    if ($proc -and -not $proc.HasExited) { try { $proc.Kill() } catch {} }
  }
}

# 1) Cert file
$okCert = Test-CertFile -Path $CertPath -Password $CertPassword
if (-not $okCert) { $failed += 'Cert file load/trust' }

# 2) User-secrets for original app
$projDir = Resolve-ProjectDir
if (-not $projDir) {
  Write-Host "Could not locate TodoApp.csproj. Checked: .\\TodoApp and .\\TodoApp\\TodoApp" -ForegroundColor Red
  $failed += 'Locate project dir'
} else {
  Write-Host ("Project directory: {0}" -f $projDir) -ForegroundColor DarkCyan
}
$okSecrets = $false
if ($projDir) { $okSecrets = Test-UserSecrets -ProjectDir $projDir }
if (-not $okSecrets) { $failed += 'User-secrets (Kestrel cert path/password)' }

# 2b) Ensure Postgres for Kestrel (uses Docker's postgres at 5433)
$pgOk = Start-DockerPostgres
if (-not $pgOk) { Write-Host "Continuing, but Kestrel may fail to start due to DB." -ForegroundColor Yellow }

# 3) Kestrel HTTPS + redirect
if (-not $SkipKestrel) {
  $kProc = $null
  if ($projDir) { $kProc = Start-KestrelApp -ProjectDir $projDir }
  try {
    if (-not (Wait-For-TlsReady -TlsHost 'localhost' -Port $KestrelHttpsPort -TimeoutSec $StartTimeoutSec)) {
      Write-Host "Kestrel HTTPS didn't become ready on :$KestrelHttpsPort" -ForegroundColor Red
      $failed += 'Kestrel HTTPS up'
    } else {
      $r1 = Test-Redirect -FromUrl ("http://localhost:{0}" -f $KestrelHttpPort) -ExpectedPrefix ("https://localhost:{0}" -f $KestrelHttpsPort)
      if (-not $r1) { $failed += 'Kestrel HTTP->HTTPS redirect' }
      $r2 = Test-CertMatchAndTrust -LocalPfx $CertPath -Password $CertPassword -TlsHost 'localhost' -Port $KestrelHttpsPort
      if (-not $r2) { $failed += 'Kestrel TLS (thumbprint/trust)' }
    }
  } finally {
    if ($kProc) { Stop-ProcessSafe -Proc $kProc }
  }
}

# 3b) IIS negative test (must fail under IIS)
$okIisNegative = $false
if ($projDir) { $okIisNegative = Test-IISNegative -ProjectDir $projDir }
if (-not $okIisNegative) { $failed += 'IIS negative test (should fail under IIS)' }

# 4) Docker HTTPS + redirect
if (-not $SkipDocker) {
  Write-Heading "Starting Docker (compose up)"
  Push-Location Docker
  try {
    docker compose up --build -d | Out-Null
    if (-not (Wait-For-TlsReady -TlsHost 'localhost' -Port $DockerHttpsPort -TimeoutSec $StartTimeoutSec)) {
      Write-Host "Docker HTTPS didn't become ready on :$DockerHttpsPort" -ForegroundColor Red
      $failed += 'Docker HTTPS up'
    } else {
      $d1 = Test-Redirect -FromUrl ("http://localhost:{0}" -f $DockerHttpPort) -ExpectedPrefix ("https://localhost:{0}" -f $DockerHttpsPort)
      if (-not $d1) {
        Write-Host "(Docker) Non-redirect response details:" -ForegroundColor Yellow
        try {
          $resp = Invoke-WebRequest ("http://localhost:{0}" -f $DockerHttpPort) -UseBasicParsing -MaximumRedirection 0
          Write-Host ("Status: {0}" -f $resp.StatusCode)
          $resp.Headers.GetEnumerator() | ForEach-Object { Write-Host ("{0}: {1}" -f $_.Key, $_.Value) }
        } catch {}
        $failed += 'Docker HTTP->HTTPS redirect'
      }
      $d2 = Test-CertMatchAndTrust -LocalPfx $CertPath -Password $CertPassword -TlsHost 'localhost' -Port $DockerHttpsPort
      if (-not $d2) { $failed += 'Docker TLS (thumbprint/trust)' }
    }
  } finally {
    Pop-Location
  }
}

Write-Heading "Summary"
if ($failed.Count -eq 0) {
  Write-Host "All HTTPS checks passed." -ForegroundColor Green
  exit 0
} else {
  Write-Host "FAILED checks:" -ForegroundColor Red
  $failed | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
  exit 1
}
