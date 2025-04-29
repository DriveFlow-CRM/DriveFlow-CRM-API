# Optimized Heroku Deployment Guide

## Deployment Overview

This project uses several optimizations to ensure reliable deployment to Heroku:

1. **Size Optimization**: 
   - `.slugignore` to exclude unnecessary files
   - Build optimizations in the project file
   - Custom build script for minimal output size

2. **Environment Variables**:
   - Environment variables are handled through Heroku config
   - Automatically copied to the correct location at runtime

## Deployment Steps

### 1. Set Environment Variables in Heroku

```bash
heroku config:set JWT_KEY=your_jwt_secret -a drive-flow-crm-api
# Set other required variables...
```

### 2. Deploy to Heroku

```bash
git add .
git commit -m "Ready for deployment"
git push heroku main
```

### 3. Monitor Logs

```bash
heroku logs --tail -a drive-flow-crm-api
```

## Manual Build (if needed)

If you need to build manually:

```bash
# Run the custom build script
./build.sh

# Deploy manually
heroku builds:create -a drive-flow-crm-api
```

## Troubleshooting

### Slug Size Issues

If you still have slug size issues:
1. Use `heroku plugins:install heroku-builds`
2. Run `heroku builds:output -a drive-flow-crm-api` to see what's taking space
3. Add more patterns to `.slugignore`

### Database Migrations

To run migrations manually:
```bash
heroku run --app drive-flow-crm-api "cd $HOME/heroku_output && dotnet DriveFlow-CRM-API.dll -- migrate"
```

## How It Works

- `Program.cs` uses `DotNetEnv.Env.Load()` to load variables from `.env`
- The Procfile includes a command that copies `.env` from the root to `DriveFlow-CRM-API/.env`
- This ensures environment variables are in the location expected by the application

## Important Notes

- The Procfile is configured to run the application directly (not using Docker)
- Environment variables are handled by DotNetEnv.Env.Load() in Program.cs
- Make sure your app has the necessary environment variables set in Heroku:
  ```bash
  heroku config:set JWT_KEY=your_jwt_secret -a drive-flow-crm-api
  ``` 