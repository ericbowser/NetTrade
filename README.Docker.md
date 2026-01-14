# Docker Setup for CoinAPI

This project includes Docker configuration to run both the backend (.NET 9.0) and frontend (React/Vite) services.

## Prerequisites

- Docker Desktop or Docker Engine installed
- Docker Compose installed

## Quick Start

1. **Build and start all services:**
   ```bash
   docker-compose up --build
   ```

2. **Run in detached mode:**
   ```bash
   docker-compose up -d --build
   ```

3. **Access the application:**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger

## Services

### Backend (coinapi-backend)
- **Port:** 8080
- **Framework:** .NET 9.0
- **Container:** `coinapi-backend`

### Frontend (coinapi-frontend)
- **Port:** 3000
- **Framework:** React + Vite
- **Web Server:** Nginx
- **Container:** `coinapi-frontend`
- **API Proxy:** Nginx proxies `/api/*` requests to the backend

## Configuration

### Backend Configuration
- Configuration file: `CoinAPI/appsettings.json`
- The appsettings.json is mounted as a volume for easy updates

### Frontend Configuration
- Development config: `www/env.json`
- Production config: `www/env.production.json`
- The production build uses relative URLs (empty BACKEND_BASE_URL) since nginx proxies API requests

## Docker Commands

### View logs
```bash
docker-compose logs -f
```

### View logs for specific service
```bash
docker-compose logs -f backend
docker-compose logs -f frontend
```

### Stop services
```bash
docker-compose down
```

### Rebuild specific service
```bash
docker-compose build backend
docker-compose build frontend
```

### Execute commands in containers
```bash
docker-compose exec backend bash
docker-compose exec frontend sh
```

## Troubleshooting

### Backend not starting
- Check that `CoinAPI/appsettings.json` exists
- Verify the .NET 9.0 SDK is available in the Docker image
- Check logs: `docker-compose logs backend`

### Frontend not connecting to backend
- Verify nginx is proxying correctly: `docker-compose logs frontend`
- Check that the backend service is running: `docker-compose ps`
- Ensure CORS is configured correctly in `Program.cs`

### Port conflicts
- If ports 3000 or 8080 are in use, update `docker-compose.yml`:
  ```yaml
  ports:
    - "3001:80"  # Change frontend port
    - "8081:8080"  # Change backend port
  ```

## Development

For local development without Docker:
- Backend: Run from `CoinAPI/` directory
- Frontend: Run `npm run dev` from `www/` directory

