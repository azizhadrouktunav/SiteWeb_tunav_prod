@echo off
REM US-GS09 - Solutions Module Setup Script (Windows)
REM This script sets up the Solutions module for ASP.NET Core backend

setlocal enabledelayedexpansion

echo ===========================================
echo US-GS09 Solutions Module Setup
echo ===========================================
echo.

REM Step 1: Start Docker
echo Step 1: Starting Docker containers...
cd ..\docker\Tunav_Website_Doc_Prod
docker-compose down
docker-compose up -d
timeout /t 5 /nobreak
echo [OK] Docker containers started
echo.

REM Step 2: Build the project
echo Step 2: Building the project...
cd ..\..\Tunav_Website_Backend_Prod
dotnet build
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)
echo [OK] Build successful
echo.

REM Step 3: Apply migrations
echo Step 3: Applying database migrations...
dotnet ef database update
if errorlevel 1 (
    echo [ERROR] Migration failed - check Docker is running and PostgreSQL is accessible
    pause
    exit /b 1
)
echo [OK] Migrations applied
echo.

REM Step 4: Run the application
echo Step 4: Starting the backend application...
echo The application will be available at:
echo   - HTTP: http://localhost:5000
echo   - HTTPS: https://localhost:5001
echo   - Swagger: http://localhost:5000/swagger
echo.
dotnet run

pause

