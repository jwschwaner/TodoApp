#!/bin/bash

# Usage: ./Add-IdentityMigration.sh <MigrationName> [Project]
if [ $# -lt 1 ]; then
    echo "Error: Migration name is required."
    echo "Usage: ./Add-IdentityMigration.sh <MigrationName> [Project]"
    exit 1
fi

MIGRATION_NAME=$1
PROJECT=${2:-TodoApp}

echo "Adding new migration '$MIGRATION_NAME' to Identity database in project '$PROJECT'..."
dotnet ef migrations add "$MIGRATION_NAME" --project "$PROJECT" --context ApplicationDbContext --output-dir Data/Migrations

echo "Migration added successfully to $PROJECT/Data/Migrations."
echo "Run ./Update-databases.sh to apply the migration."
