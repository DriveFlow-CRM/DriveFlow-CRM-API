# Simple Heroku Deployment Guide

## Environment Variable Handling

This project uses a two-step approach for environment variables:

1. **Local Development**: Environment variables are read from `DriveFlow-CRM-API/.env`
2. **Heroku Deployment**: 
   - Environment variables are set in Heroku's config
   - The Procfile automatically copies `.env` to `DriveFlow-CRM-API/.env` at startup

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