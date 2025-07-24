#!/bin/bash

echo "Setting up LinkedIn Game Scores Docker environment..."

# Create data directory for PostgreSQL
echo "Creating PostgreSQL data directory..."
mkdir -p ./data/postgres

# Set permissions (Linux/Mac)
if [[ "$OSTYPE" == "linux-gnu"* ]] || [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Setting permissions for PostgreSQL data directory..."
    sudo chown -R 999:999 ./data/postgres
    chmod -R 755 ./data/postgres
fi

echo "Setup complete! You can now run: docker-compose up -d"