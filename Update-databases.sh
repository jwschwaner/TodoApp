#!/bin/bash

# Update both database contexts (Identity and Todo)
echo "Updating Identity database..."
dotnet ef database update --project TodoApp --context ApplicationDbContext

echo "Updating Todo database..."
dotnet ef database update --project TodoApp --context TodoDbContext

echo "Database updates completed."
