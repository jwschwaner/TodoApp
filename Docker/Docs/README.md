# TodoApp Docker Deployment Commands

Prerequisites:

- Docker Desktop installed and running
- Certificate file (todoapp-cert.pfx) in the Docker directory
- Navigate to the Docker directory before running these commands

## Docker CLI Commands to Deploy TodoApp:

1. Navigate to Docker directory:
   cd Docker

2. Ensure certificate is present:
   ls -la todoapp-cert.pfx

3. Build the Docker image:
   docker build -t todoapp-https .

4. Start the application with PostgreSQL database:
   docker-compose up -d

5. Check if containers are running:
   docker ps

6. View application logs:
   docker logs todoapp-docker-https

7. View database logs:
   docker logs todoapp-docker-postgres

## Access the Application:

- HTTPS: https://localhost:5003
- HTTP: http://localhost:5002 (redirects to HTTPS)

## Wait for Database Initialization:

The PostgreSQL container needs a few seconds to initialize the databases.
If you get database connection errors initially, wait 10-15 seconds and try again.

## Stopping the Application:

docker-compose down

## Cleanup (removes containers and images):

docker-compose down --rmi all --volumes

## Troubleshooting:

- If build fails, ensure todoapp-cert.pfx is in the Docker directory
- If database connection fails, wait a few seconds for PostgreSQL to start
- Check logs with: docker logs [container-name]
- Verify network connectivity: docker network ls
- If certificate issues, verify the certificate file exists and has correct permissions

## Database Access (for debugging):

docker exec -it todoapp-docker-postgres psql -U postgres -d Identity
docker exec -it todoapp-docker-postgres psql -U postgres -d Todo

## External database access (from host machine):

psql -h localhost -p 5433 -U postgres -d Identity
psql -h localhost -p 5433 -U postgres -d Todo

## Complete Rebuild (if needed):

docker-compose down --rmi all --volumes
docker system prune -f
docker build --no-cache -t todoapp-https .
docker-compose up -d
