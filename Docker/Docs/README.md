# TodoApp Docker (Development) - Compose Deployment

Prerequisites
- Docker Desktop installed and running
- You are in the Docker directory of the repo
- A trusted dev cert exists on the host (created with `dotnet dev-certs https -ep %USERPROFILE%\todoapp-cert.pfx -p "TodoApp2024!"` and trusted)

Quick start
```powershell
# Windows PowerShell
cd Docker
docker compose up --build -d
```

```bash
# macOS/Linux
cd Docker
# Adjust volume path in docker-compose.yml if needed, then:
docker compose up --build -d
```

What this does
- Builds the app image (ASPNETCORE_ENVIRONMENT=Development in the container)
- Starts PostgreSQL (port 5433 on host -> 5432 in container) and initializes databases
- Runs TodoApp with ports:
  - HTTP: 5002 -> 5204 (redirects to HTTPS)
  - HTTPS: 5003 -> 5003 (container listens on 5003)
- Mounts the host’s trusted dev cert into the container at `/app/certs/todoapp-cert.pfx` so browsers show a secure connection

Access the app
- HTTPS: https://localhost:5003
- HTTP:  http://localhost:5002

Troubleshooting
- If the browser still shows “Not secure”:
  1) Ensure `%USERPROFILE%\todoapp-cert.pfx` exists and is trusted on Windows:
     ```powershell
     dotnet dev-certs https --trust
     ```
  2) Rebuild and restart containers:
     ```powershell
     docker compose down
     docker compose up --build -d
     ```
  3) Verify the cert is mounted in the container:
     ```powershell
     docker exec -it todoapp-docker-https ls -l /app/certs/todoapp-cert.pfx
     ```
  4) If the mount fails on Windows, replace the `${USERPROFILE}` mapping in docker-compose.yml with an absolute path using forward slashes, e.g.:
     ```
     C:/Users/<YourUser>/todoapp-cert.pfx:/app/certs/todoapp-cert.pfx:ro
     ```
- If ports 5002/5003/5433 are busy on the host, change them in docker-compose.yml and re-run.

Cleanup
```powershell
docker compose down
```
