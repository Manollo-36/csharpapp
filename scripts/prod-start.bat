@echo off
REM Build and run the application in production mode

echo 🐳 Starting CSharpApp in Production Mode...

REM Stop any existing containers
echo Stopping existing containers...
docker-compose down

REM Build and start the application
echo Building and starting containers...
docker-compose up --build -d

REM Wait for the application to start
echo Waiting for application to start...
timeout /t 15 /nobreak >nul

REM Check health
echo Checking application health...
curl -f http://localhost:5225/health

if %errorlevel% equ 0 (
    echo ✅ Application is running successfully in production mode!
    echo 🌐 API available at: http://localhost:5225
    echo 📊 Performance metrics: http://localhost:5225/api/v1.0/performance/metrics
    echo 🏥 Health check: http://localhost:5225/health
    echo.
    echo 📝 View logs with: docker-compose logs -f
    echo 🛑 Stop with: docker-compose down
) else (
    echo ❌ Application failed to start. Check logs:
    docker-compose logs csharpapp-api
)

pause