# TodoApp - HTTPS-ready Blazor Server (Kestrel + Docker)

This repo is configured to run securely over HTTPS both locally (Kestrel) and in a Docker Linux container. Follow these steps to get to a testable state fast.

## Prerequisites (Windows)

- .NET 9 SDK
- Docker Desktop

## Quick start (automated verification)

```powershell
cd C:\Repos\TodoApp
# Run the full HTTPS verification (cert + secrets + Kestrel + Docker + IIS-negative)
./Test-HttpsRequirements.ps1
```

Expected: "All HTTPS checks passed." and both endpoints work:

- Kestrel: https://localhost:7181 (HTTP 5204 redirects)
- Docker:  https://localhost:5003 (HTTP 5002 redirects)

## Manual steps (Kestrel)

1. Create a dev cert and trust it, then set user-secrets

   ```powershell
   cd C:\Repos\TodoApp
   ./Create-DevCert.ps1
   ./Set-UserSecrets.ps1
   ```

2. Run the app on Kestrel

   ```powershell
   cd .\TodoApp
   dotnet run
   ```

Open:

- HTTPS: https://localhost:7181
- HTTP:  http://localhost:5204 (redirects to HTTPS)

Note: The app is intentionally locked to Kestrel. If run under IIS, it fails fast with a clear error.

## Manual steps (Docker)

1. Build and run with compose

   ```powershell
   cd C:\Repos\TodoApp\Docker
   docker compose up --build -d
   ```

2. Browse the containerized app

- HTTPS: https://localhost:5003
- HTTP:  http://localhost:5002 (redirects to HTTPS)

If the browser shows "Not secure":

- Ensure the host PFX exists at %USERPROFILE%\todoapp-cert.pfx and is trusted: `dotnet dev-certs https --trust`
- If the volume mount fails, change the mapping in Docker/docker-compose.yml to an absolute Windows path with forward slashes, e.g.:
  - `C:/Users/<YourUser>/todoapp-cert.pfx:/app/certs/host/todoapp-cert.pfx:ro`

## What’s configured

- HTTPS redirect enforced in both copies.
- Kestrel (original app): HTTPS cert path/password read from user-secrets; locked to Kestrel (IIS negative test will fail as required).
- Docker (Linux): Container listens on HTTPS 5003; uses the mounted host dev cert at /app/certs/host/todoapp-cert.pfx (trusted on host) so browsers show a secure padlock.

## Automated test script (optional but recommended)

```powershell
cd C:\Repos\TodoApp
# Full suite (fast): cert presence/trust, user-secrets, Kestrel HTTPS+redirect, IIS negative, Docker HTTPS+redirect
./Test-HttpsRequirements.ps1
```

## Troubleshooting

- Firefox uses its own CA store; enable `about:config -> security.enterprise_roots.enabled = true`, or import the dev cert.
- Port conflicts: change host ports in Docker/docker-compose.yml and re-run `docker compose up -d`.
- To restart clean: `docker compose down` (in Docker folder), then `docker compose up --build -d`.

## See also (Docker details)

- Docker/Docs/README.md (canonical Docker guide in this repo)
