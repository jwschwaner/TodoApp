#!/bin/bash

# Remove the most recent migration from the Identity context
echo "Removing the latest migration from Identity database..."
dotnet ef migrations remove --project TodoApp --context ApplicationDbContext

echo "Migration removed successfully."
