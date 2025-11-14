@echo off
REM Build and run the application in development mode

echo 🐳 Starting CSharpApp in Development Mode...

REM Stop any existing containers
echo Stopping existing containers...
docker-compose -f docker-compose.dev.yml down

REM Build and start the application
echo Building and starting containers...
docker-compose -f docker-compose.dev.yml up --build -d

REM Wait for the application to start
echo Waiting for application to start...
timeout /t 10 /nobreak >nul

REM Check health
echo Checking application health...
curl -f http://localhost:5225/health

if %errorlevel% equ 0 (
    echo ✅ Application is running successfully!
    echo 🌐 API available at: http://localhost:5225
    echo 📊 Performance metrics: http://localhost:5225/api/v1.0/performance/metrics
    echo 🏥 Health check: http://localhost:5225/health
    echo.
    echo 📋 Available endpoints:
    echo   - Products: http://localhost:5225/api/v1.0/products
    echo   - Categories: http://localhost:5225/api/v1.0/categories
    echo   - Auth Status: http://localhost:5225/api/v1.0/auth/status
    echo.
    echo 📝 View logs with: docker-compose -f docker-compose.dev.yml logs -f
    echo 🛑 Stop with: docker-compose -f docker-compose.dev.yml down
) else (
    echo ❌ Application failed to start. Check logs:
    docker-compose -f docker-compose.dev.yml logs csharpapp-api
)

pause