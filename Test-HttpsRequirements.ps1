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

function Write-TestCaseResult {
  param(
    [string]$Name,
    [string]$Expected,
    [string]$Actual,
    [bool]$Pass
  )
  Write-Host "[TEST] $Name" -ForegroundColor White
  Write-Host " - Expected: $Expected" -ForegroundColor DarkGray
  Write-Host " - Actual:   $Actual" -ForegroundColor DarkGray
  if ($Pass) {
    Write-Host " - Result:   PASS" -ForegroundColor Green
  } else {
    Write-Host " - Result:   FAIL" -ForegroundColor Red
  }
}

function Test-CertFile {
  param([string]$Path,[string]$Password)
  Write-Heading "Checking cert file in user folder"
  $result = [ordered]@{ Name='Cert file load/trust'; Pass=$false; Subject=''; Trusted=$false; Path=$Path; Error=$null }
  if (-not (Test-Path $Path)) {
    $result.Error = "Missing: $Path"
    return $result
  }
  try {
    $pfx = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $Path, $Password
    $chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
    $trusted = $chain.Build($pfx)
    $result.Subject = $pfx.Subject
    $result.Trusted = $trusted
    $result.Pass = $true
  } catch {
    $result.Error = $_.Exception.Message
  }
  return $result
}

function Test-UserSecrets {
  param([string]$ProjectDir)
  Write-Heading "Checking user-secrets for Kestrel cert path/password"
  $res = [ordered]@{ Name='User-secrets has Kestrel cert path/password'; Pass=$false; HasPath=$false; HasPassword=$false; ProjectDir=$ProjectDir }
  Push-Location $ProjectDir
  try {
    $out = dotnet user-secrets list 2>$null
    if ($out) {
      $res.HasPath = [bool]($out -match 'Kestrel:Certificates:Default:Path')
      $res.HasPassword = [bool]($out -match 'Kestrel:Certificates:Default:Password')
      $res.Pass = ($res.HasPath -and $res.HasPassword)
    }
  } finally {
    Pop-Location
  }
  return $res
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
        if ($ssl.IsAuthenticated) { return @{ Pass=$true; Ms=[int]$sw.Elapsed.TotalMilliseconds } }
      } finally { if ($ssl) { $ssl.Dispose() }; $tcp.Dispose() }
    } catch { }
    Start-Sleep -Milliseconds 300
  }
  return @{ Pass=$false; Ms=[int]$sw.Elapsed.TotalMilliseconds }
}

function Test-Redirect {
  param([string]$FromUrl,[string]$ExpectedPrefix)
  $resp = $null
  $status = $null
  $location = $null
  try {
    $resp = Invoke-WebRequest $FromUrl -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    $status = [int]$resp.StatusCode
    $location = $resp.Headers['Location']
  } catch {
    if ($_.Exception.Response) {
      $resp = $_.Exception.Response
      $status = [int]$resp.StatusCode
      $location = $resp.Headers['Location']
    }
  }
  $pass = ($resp -ne $null -and $status -ge 300 -and $status -lt 400 -and $location -like "$ExpectedPrefix*")
  return @{ Name='HTTP->HTTPS redirect'; Pass=$pass; Status=$status; Location=$location; From=$FromUrl; ExpectedPrefix=$ExpectedPrefix }
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
  return @{ Name='TLS thumbprint match + OS trust'; Pass=($match -and $trusted); LocalThumb=$local.Thumbprint; RemoteThumb=$remote.Thumbprint; Trusted=$trusted; Host=$TlsHost; Port=$Port }
}

function Start-DockerPostgres {
  Write-Heading "Ensuring Docker Postgres is running (port 5433) for Kestrel tests"
  Push-Location Docker
  try {
    docker compose up -d postgres | Out-Null
  } finally {
    Pop-Location
  }
  $deadline = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $deadline) {
    if ((Test-NetConnection localhost -Port 5433).TcpTestSucceeded) { return $true }
    Start-Sleep -Milliseconds 250
  }
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
  $candidates = @(
    (Join-Path $PSScriptRoot 'TodoApp'),
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
  $psi.Environment['IIS_SITE_NAME'] = 'TestSite'
  $psi.Environment['ASPNETCORE_IIS_HTTPAUTH'] = '1'
  $psi.Environment['ASPNETCORE_ENVIRONMENT'] = 'Development'
  $psi.Environment['ConnectionStrings__IdentityConnection'] = 'Host=localhost;Port=5433;Database=Identity;Username=postgres;Password=postgres'
  $psi.Environment['ConnectionStrings__TodoConnection']     = 'Host=localhost;Port=5433;Database=Todo;Username=postgres;Password=postgres'

  $proc = [System.Diagnostics.Process]::Start($psi)
  try {
    $exited = $proc.WaitForExit($TimeoutSec * 1000)
    if (-not $exited) {
      try { $proc.Kill() } catch {}
      return @{ Name='IIS negative (locked to Kestrel)'; Pass=$false; ExitCode=$null; Reason='Timeout' }
    }
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $combined = "$stdout`n$stderr"
    $failedAsExpected = ($proc.ExitCode -ne 0) -and ($combined -match 'locked to the cross-platform Kestrel web server')
    return @{ Name='IIS negative (locked to Kestrel)'; Pass=$failedAsExpected; ExitCode=$proc.ExitCode; MatchedMessage=($combined -match 'locked to the cross-platform Kestrel web server') }
  } finally {
    if ($proc -and -not $proc.HasExited) { try { $proc.Kill() } catch {} }
  }
}

# 1) Cert file
$certRes = Test-CertFile -Path $CertPath -Password $CertPassword
$certActual = if ($certRes.Pass) { "Loaded '$($certRes.Subject)'; Trusted=$($certRes.Trusted)" } else { $certRes.Error }
Write-TestCaseResult -Name $certRes.Name -Expected "PFX exists at $($certRes.Path), loads without error, OS trust true/ok" -Actual $certActual -Pass $certRes.Pass
if (-not $certRes.Pass) { $failed += $certRes.Name }

# 2) User-secrets for original app
$projDir = Resolve-ProjectDir
if (-not $projDir) {
  Write-Host "Project directory not found (checked .\\TodoApp and .\\TodoApp\\TodoApp)" -ForegroundColor Red
  $failed += 'Locate project dir'
} else {
  Write-Host ("Project directory: {0}" -f $projDir) -ForegroundColor DarkCyan
}
$secretRes = $null
if ($projDir) { $secretRes = Test-UserSecrets -ProjectDir $projDir }
if ($secretRes) {
  $secretActual = "HasPath=$($secretRes.HasPath), HasPassword=$($secretRes.HasPassword)"
  Write-TestCaseResult -Name $secretRes.Name -Expected "Keys exist in user-secrets (no plaintext shown)" -Actual $secretActual -Pass $secretRes.Pass
  if (-not $secretRes.Pass) { $failed += $secretRes.Name }
}

# 2b) Ensure Postgres for Kestrel
$pgOk = Start-DockerPostgres
Write-TestCaseResult -Name 'Docker Postgres running for local Kestrel' -Expected '5433 accepts TCP' -Actual ((Test-NetConnection localhost -Port 5433).TcpTestSucceeded) -Pass $pgOk
if (-not $pgOk) { Write-Host "Continuing, but Kestrel may fail to start due to DB." -ForegroundColor Yellow }

# 3) Kestrel HTTPS + redirect
if (-not $SkipKestrel) {
  $kProc = $null
  if ($projDir) { $kProc = Start-KestrelApp -ProjectDir $projDir }
  try {
    $ready = Wait-For-TlsReady -TlsHost 'localhost' -Port $KestrelHttpsPort -TimeoutSec $StartTimeoutSec
    $kReadyPass = [bool]$ready.Pass
    $kReadyActual = if ($kReadyPass) { "TLS handshake ok in $($ready.Ms) ms on :$KestrelHttpsPort" } else { "Not ready after $($ready.Ms) ms on :$KestrelHttpsPort" }
    Write-TestCaseResult -Name 'Kestrel HTTPS up' -Expected "TLS on https://localhost:$KestrelHttpsPort within ${StartTimeoutSec}s" -Actual $kReadyActual -Pass $kReadyPass
    if (-not $kReadyPass) { $failed += 'Kestrel HTTPS up' }
    if ($kReadyPass) {
      $r1 = Test-Redirect -FromUrl ("http://localhost:{0}" -f $KestrelHttpPort) -ExpectedPrefix ("https://localhost:{0}" -f $KestrelHttpsPort)
      $r1Actual = "Status=$($r1.Status), Location=$($r1.Location)"
      Write-TestCaseResult -Name "Kestrel $($r1.Name)" -Expected ("3xx to https://localhost:{0}/" -f $KestrelHttpsPort) -Actual $r1Actual -Pass $r1.Pass
      if (-not $r1.Pass) { $failed += 'Kestrel HTTP->HTTPS redirect' }

      $r2 = Test-CertMatchAndTrust -LocalPfx $CertPath -Password $CertPassword -TlsHost 'localhost' -Port $KestrelHttpsPort
      $r2Actual = "Local=$($r2.LocalThumb), Remote=$($r2.RemoteThumb), Trusted=$($r2.Trusted)"
      Write-TestCaseResult -Name "Kestrel $($r2.Name)" -Expected 'Thumbprints equal and OS trust true' -Actual $r2Actual -Pass $r2.Pass
      if (-not $r2.Pass) { $failed += 'Kestrel TLS (thumbprint/trust)' }
    }
  } finally {
    if ($kProc) { Stop-ProcessSafe -Proc $kProc }
  }
}

# 3b) IIS negative test (must fail under IIS)
$okIisNegative = $false
if ($projDir) {
  $iisRes = Test-IISNegative -ProjectDir $projDir
  $okIisNegative = [bool]$iisRes.Pass
  $iisActual = if ($iisRes.Reason) { $iisRes.Reason } else { "ExitCode=$($iisRes.ExitCode), MatchedMessage=$($iisRes.MatchedMessage)" }
  Write-TestCaseResult -Name $iisRes.Name -Expected 'Non-zero exit and clear message about Kestrel-only' -Actual $iisActual -Pass $okIisNegative
}
if (-not $okIisNegative) { $failed += 'IIS negative test (should fail under IIS)' }

# 4) Docker HTTPS + redirect
if (-not $SkipDocker) {
  Write-Heading "Starting Docker (compose up)"
  Push-Location Docker
  try {
    docker compose up --build -d | Out-Null
    $dReady = Wait-For-TlsReady -TlsHost 'localhost' -Port $DockerHttpsPort -TimeoutSec $StartTimeoutSec
    $dReadyPass = [bool]$dReady.Pass
    $dReadyActual = if ($dReadyPass) { "TLS handshake ok in $($dReady.Ms) ms on :$DockerHttpsPort" } else { "Not ready after $($dReady.Ms) ms on :$DockerHttpsPort" }
    Write-TestCaseResult -Name 'Docker HTTPS up' -Expected "TLS on https://localhost:$DockerHttpsPort within ${StartTimeoutSec}s" -Actual $dReadyActual -Pass $dReadyPass
    if (-not $dReadyPass) { $failed += 'Docker HTTPS up' }
    if ($dReadyPass) {
      $d1 = Test-Redirect -FromUrl ("http://localhost:{0}" -f $DockerHttpPort) -ExpectedPrefix ("https://localhost:{0}" -f $DockerHttpsPort)
      $d1Actual = "Status=$($d1.Status), Location=$($d1.Location)"
      Write-TestCaseResult -Name "Docker $($d1.Name)" -Expected ("3xx to https://localhost:{0}/" -f $DockerHttpsPort) -Actual $d1Actual -Pass $d1.Pass
      if (-not $d1.Pass) { $failed += 'Docker HTTP->HTTPS redirect' }

      $d2 = Test-CertMatchAndTrust -LocalPfx $CertPath -Password $CertPassword -TlsHost 'localhost' -Port $DockerHttpsPort
      $d2Actual = "Local=$($d2.LocalThumb), Remote=$($d2.RemoteThumb), Trusted=$($d2.Trusted)"
      Write-TestCaseResult -Name "Docker $($d2.Name)" -Expected 'Thumbprints equal and OS trust true' -Actual $d2Actual -Pass $d2.Pass
      if (-not $d2.Pass) { $failed += 'Docker TLS (thumbprint/trust)' }
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
