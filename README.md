# TodoApp - Secure Blazor Todo Application

A modern Blazor Server application with ASP.NET Core Identity, two-factor authentication, role-based authorization, and HTTPS certificate configuration.

## Features

- **User Authentication & Authorization**: Complete ASP.NET Core Identity implementation
- **Two-Factor Authentication**: Authenticator app support with QR code generation
- **Role-Based Access**: Admin and User roles with different permissions
- **Secure HTTPS**: Self-signed certificate configuration for development
- **Dual Database Architecture**: Separate databases for Identity and Todo data
- **Docker Support**: Full containerization with PostgreSQL
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Prerequisites

### Required Software

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) (if running locally)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for containerized deployment)

### Platform-Specific Prerequisites

#### Windows
- Visual Studio 2022 (optional, but recommended)
- Windows PowerShell or Command Prompt
- IIS (for testing IIS vs Kestrel behavior)

#### macOS
- Terminal
- Homebrew (recommended for package management)

#### Linux
- Terminal with bash support

## Certificate Setup

### Step 1: Create Self-Signed Certificate

The application requires a self-signed HTTPS certificate stored in your user home directory.

#### Windows (PowerShell)
```powershell
# Navigate to user home directory
cd $env:USERPROFILE

# Create self-signed certificate
dotnet dev-certs https -ep todoapp-cert.pfx -p "TodoApp2024!"

# Verify certificate creation
dir todoapp-cert.pfx
```

#### macOS/Linux (Terminal)
```bash
# Navigate to user home directory
cd ~

# Create self-signed certificate (use single quotes to avoid shell expansion)
dotnet dev-certs https -ep ~/todoapp-cert.pfx -p 'TodoApp2024!'

# Alternative: escape the exclamation mark
# dotnet dev-certs https -ep ~/todoapp-cert.pfx -p "TodoApp2024\!"

# Verify certificate creation
ls -la ~/todoapp-cert.pfx
```

### Step 2: Configure User Secrets

User secrets provide a secure way to store the certificate path and password during development.

#### Windows (PowerShell)
```powershell
# Navigate to the TodoApp project directory
cd path\to\TodoApp

# Initialize user secrets
dotnet user-secrets init

# Set certificate path and password
dotnet user-secrets set "Kestrel:Certificates:Default:Path" "$env:USERPROFILE\todoapp-cert.pfx"
dotnet user-secrets set "Kestrel:Certificates:Default:Password" "TodoApp2024!"

# Verify secrets are set
dotnet user-secrets list
```

#### macOS/Linux (Terminal)
```bash
# Navigate to the TodoApp project directory
cd path/to/TodoApp

# Initialize user secrets
dotnet user-secrets init

# Set certificate path and password (use single quotes for password)
dotnet user-secrets set "Kestrel:Certificates:Default:Path" "~/todoapp-cert.pfx"
dotnet user-secrets set "Kestrel:Certificates:Default:Password" 'TodoApp2024!'

# Verify secrets are set
dotnet user-secrets list
```

## Database Setup

### Local PostgreSQL Setup

1. **Install PostgreSQL** on your system
2. **Start PostgreSQL service**
3. **Create databases** using the provided script:

#### Windows (PowerShell)
```powershell
# Start Docker with PostgreSQL
docker-compose up -d

# Or manually create databases in PostgreSQL
psql -U postgres -c "CREATE DATABASE \"Identity\";"
psql -U postgres -c "CREATE DATABASE \"Todo\";"
```

#### macOS/Linux (Terminal)
```bash
# Start Docker with PostgreSQL
docker-compose up -d

# Or manually create databases in PostgreSQL
psql -U postgres -c "CREATE DATABASE \"Identity\";"
psql -U postgres -c "CREATE DATABASE \"Todo\";"
```

### Apply Database Migrations

#### Windows (PowerShell)
```powershell
# Update both databases
.\Update-databases.ps1

# Or manually:
dotnet ef database update --project TodoApp --context ApplicationDbContext
dotnet ef database update --project TodoApp --context TodoDbContext
```

#### macOS/Linux (Terminal)
```bash
# Make script executable and run
chmod +x Update-databases.sh
./Update-databases.sh

# Or manually:
dotnet ef database update --project TodoApp --context ApplicationDbContext
dotnet ef database update --project TodoApp --context TodoDbContext
```

## Running the Application

### Local Development (Kestrel)

#### Windows (PowerShell)
```powershell
cd TodoApp
dotnet run
```

#### macOS/Linux (Terminal)
```bash
cd TodoApp
dotnet run
```

The application will be available at:
- HTTP: `http://localhost:5204`
- HTTPS: `https://localhost:7181`

### Docker Deployment

#### Prerequisites for Docker
- Ensure Docker Desktop is installed and running
- Certificate must be accessible within the container

#### Windows (PowerShell)
```powershell
# Navigate to Docker folder
cd Docker

# Build the Docker image
docker build -t todoapp-https .

# Run with docker-compose (includes PostgreSQL)
docker-compose up -d

# Check container status
docker ps

# View logs
docker logs todoapp-https
```

#### macOS/Linux (Terminal)
```bash
# Navigate to Docker folder
cd Docker

# Build the Docker image
docker build -t todoapp-https .

# Run with docker-compose (includes PostgreSQL)
docker-compose up -d

# Check container status
docker ps

# View logs
docker logs todoapp-https
```

The containerized application will be available at:
- HTTPS: `https://localhost:5001`

## Configuration

### Connection Strings

Update `appsettings.json` with your PostgreSQL connection details:

```json
{
  "ConnectionStrings": {
    "IdentityConnection": "Host=localhost;Port=5432;Database=Identity;Username=postgres;Password=postgres",
    "TodoConnection": "Host=localhost;Port=5432;Database=Todo;Username=postgres;Password=postgres"
  }
}
```

### HTTPS Configuration

The application is configured to:
- Automatically redirect HTTP to HTTPS
- Use the self-signed certificate from user secrets
- Run on Kestrel server (IIS integration disabled)

## User Roles

The application includes two predefined roles:

### Admin Role
- Full access to user management
- Can view and manage all users' todos
- Access to admin panel at `/admin/users`

### User Role
- Can manage their own todos
- Must register CPR number before accessing todos
- Standard user functionality

## Troubleshooting

### Common Issues

#### Certificate Not Found
```
Error: Unable to configure HTTPS endpoint
```
**Solution**: Ensure the certificate exists in your user home directory and user secrets are configured correctly.

#### Database Connection Issues
```
Error: A connection was not established
```
**Solution**: 
1. Verify PostgreSQL is running
2. Check connection strings in `appsettings.json`
3. Ensure databases exist

#### Docker Build Fails
```
Error: Certificate not accessible in container
```
**Solution**: 
1. Ensure certificate is copied into Docker image
2. Check Dockerfile paths
3. Verify certificate permissions

### Verification Steps

#### 1. Test HTTPS Redirect
```bash
curl -I http://localhost:5204
# Should return 301/302 redirect to https://
```

#### 2. Test Certificate Loading
```bash
curl -k https://localhost:7181
# Should return HTML content without certificate errors
```

#### 3. Test Database Connectivity
- Register a new user
- Log in and create todos
- Verify admin functionality

## Development Commands

### Entity Framework Migrations

#### Add New Migration
```bash
# Identity context
dotnet ef migrations add MigrationName --project TodoApp --context ApplicationDbContext

# Todo context  
dotnet ef migrations add MigrationName --project TodoApp --context TodoDbContext --output-dir TodoData/Migrations
```

#### Remove Last Migration
```bash
# Identity context
dotnet ef migrations remove --project TodoApp --context ApplicationDbContext

# Todo context
dotnet ef migrations remove --project TodoApp --context TodoDbContext
```

### Docker Commands

#### Build Image
```bash
docker build -t todoapp-https .
```

#### Run Container
```bash
docker run -d -p 5001:5001 --name todoapp-container todoapp-https
```

#### Stop and Clean Up
```bash
docker-compose down
docker rmi todoapp-https
```

## Project Structure

```
TodoApp/
├── Components/                 # Blazor components
│   ├── Account/               # Authentication components
│   ├── Layout/                # Layout components
│   └── Pages/                 # Application pages
├── Data/                      # Identity data context
├── TodoData/                  # Todo data context and models
├── Migrations/                # Entity Framework migrations
├── wwwroot/                   # Static web assets
├── Docker/                    # Docker deployment files
├── Docs/                      # Documentation
└── README.md                  # This file
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is for educational purposes.

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review the configuration steps
3. Ensure all prerequisites are met
4. Verify certificate and database setup
