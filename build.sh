#!/bin/bash

# Custom build script for Heroku deployment
echo "Building DriveFlow CRM API for Heroku..."

# Set environment variables
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Navigate to the project directory
cd DriveFlow-CRM-API

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore --runtime linux-x64

# Publish the application with optimizations
echo "Publishing application..."
dotnet publish --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output $HOME/heroku_output \
  -p:PublishTrimmed=true \
  -p:PublishSingleFile=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -p:EnableCompressionInSingleFile=true

# Copy environment variables file if it exists
if [ -f "../.env" ]; then
  echo "Copying environment file..."
  cp -f ../.env $HOME/heroku_output/.env
fi

echo "Build completed successfully." 