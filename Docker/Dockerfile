# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY TodoApp/*.csproj ./TodoApp/
RUN dotnet restore "TodoApp/TodoApp.csproj"

# Copy all source code
COPY TodoApp/ ./TodoApp/
WORKDIR "/src/TodoApp"

# Build and publish the application
RUN dotnet publish "TodoApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 9.0 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Create certificate directory and copy certificate
RUN mkdir -p /app/certs
COPY todoapp-cert.pfx /app/certs/

# Set environment variables for certificate - Development environment inside container
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS="https://0.0.0.0:5003;http://0.0.0.0:5204"
ENV Kestrel__Certificates__Default__Path="/app/certs/todoapp-cert.pfx"
ENV Kestrel__Certificates__Default__Password="TodoApp2024!"

# Expose the ports the application actually uses
EXPOSE 5204 5003

ENTRYPOINT ["dotnet", "TodoApp.dll"]