# Manual Docker-based Heroku Deployment

## How It Works

This project uses Docker to deploy to Heroku, avoiding SDK version compatibility issues:

1. **Docker Container**:
   - The `Dockerfile` defines a multi-stage build process using .NET SDK 8.0
   - First stage builds the application
   - Second stage creates a minimal runtime image
   - A startup script ensures environment variables are properly placed

2. **Heroku Integration**:
   - `heroku.yml` tells Heroku to use the Docker container 
   - Heroku automatically builds the Docker image when you push
   - Runtime configuration is handled through Heroku's config system

3. **Environment Variables**:
   - Heroku config variables are automatically available to the container
   - The startup script copies environment files to the right location

## Manual Deployment Steps

### 1. Set Up Heroku Container Stack

One-time setup to use Docker containers:

```bash
# Set the stack to container
heroku stack:set container -a drive-flow-crm-api
```

### 2. Set Environment Variables

```bash
# Set required environment variables
heroku config:set JWT_KEY=your_jwt_secret -a drive-flow-crm-api
# Set other variables as needed...
```

### 3. Deploy with Git Push

```bash
# Add and commit your changes
git add .
git commit -m "Ready for Docker deployment"

# Push to Heroku
git push heroku main
```

### 4. Monitor the Deployment

```bash
# Watch the build logs
heroku logs --tail -a drive-flow-crm-api
```

## Benefits of this Approach

- **Consistent Environment**: The same Docker image runs locally and in production
- **SDK Version Control**: Avoids Heroku buildpack SDK version issues
- **Simplified Management**: No need for custom scripts or buildpack configurations

## Troubleshooting

If you encounter issues:

1. **View Build Logs**:
   ```bash
   heroku builds:info -a drive-flow-crm-api
   ```

2. **Debug the Container**:
   ```bash
   heroku run bash -a drive-flow-crm-api
   ```

3. **Restart the App**:
   ```bash
   heroku restart -a drive-flow-crm-api
   ```

## Important Notes

- The Procfile is configured to run the application directly (not using Docker)
- Environment variables are handled by DotNetEnv.Env.Load() in Program.cs
- Make sure your app has the necessary environment variables set in Heroku:
  ```bash
  heroku config:set JWT_KEY=your_jwt_secret -a drive-flow-crm-api
  ``` 