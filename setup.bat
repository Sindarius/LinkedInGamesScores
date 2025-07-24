@echo off
echo Setting up LinkedIn Game Scores Docker environment...

echo Creating PostgreSQL data directory...
if not exist "data\postgres" mkdir "data\postgres"

echo Setup complete! You can now run: docker-compose up -d
pause