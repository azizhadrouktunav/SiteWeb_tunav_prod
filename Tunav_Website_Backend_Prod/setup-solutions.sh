#!/bin/bash
# US-GS09 - Solutions Module Setup Script
# This script sets up the Solutions module for ASP.NET Core backend

echo "=== US-GS09 Solutions Module Setup ==="
echo ""

# Step 1: Start Docker
echo "Step 1: Starting Docker containers..."
cd ../Tunav_Website_Doc_Prod
docker-compose down
docker-compose up -d
sleep 5
echo "✓ Docker containers started"
echo ""

# Step 2: Build the project
echo "Step 2: Building the project..."
cd ../../Tunav_Website_Backend_Prod
dotnet build
echo "✓ Build successful"
echo ""

# Step 3: Apply migrations
echo "Step 3: Applying database migrations..."
dotnet ef database update
echo "✓ Migrations applied"
echo ""

# Step 4: Run the application
echo "Step 4: Starting the backend application..."
echo "The application will be available at:"
echo "  - HTTP: http://localhost:5000"
echo "  - HTTPS: https://localhost:5001"
echo "  - Swagger: http://localhost:5000/swagger"
echo ""
dotnet run

