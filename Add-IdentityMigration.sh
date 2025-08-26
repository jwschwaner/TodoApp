#!/bin/bash

# Check if migration name is provided
if [ $# -eq 0 ]; then
    echo "Error: Migration name is required."
    echo "Usage: ./Add-IdentityMigration.sh <MigrationName>"
    exit 1
fi

MIGRATION_NAME=$1

# Add a new migration to the Identity context
echo "Adding new migration '$MIGRATION_NAME' to Identity database..."
dotnet ef migrations add $MIGRATION_NAME --project TodoApp --context ApplicationDbContext

echo "Migration added successfully."
echo "Run ./Update-databases.sh to apply the migration."
