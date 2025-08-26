#!/bin/bash

# Remove the most recent migration from the Todo context
echo "Removing the latest migration from Todo database..."
dotnet ef migrations remove --project TodoApp --context TodoDbContext

echo "Migration removed successfully."
