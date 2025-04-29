# Simple Heroku Deployment Guide

## Prerequisites
- Heroku CLI
- Git

## Setup

1. **Environment Variables**
   Run the setup script to create the .env file in the correct location:
   ```bash
   ./setup-env.sh
   ```

2. **Deploy to Heroku**
   ```bash
   git add .
   git commit -m "Ready for deployment"
   git push heroku main
   ```

3. **Monitor Logs**
   ```bash
   heroku logs --tail -a drive-flow-crm-api
   ```

## Important Notes

- The Procfile is configured to run the application directly (not using Docker)
- Environment variables are handled by DotNetEnv.Env.Load() in Program.cs
- Make sure your app has the necessary environment variables set in Heroku:
  ```bash
  heroku config:set JWT_KEY=your_jwt_secret -a drive-flow-crm-api
  ``` 