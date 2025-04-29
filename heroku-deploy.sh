#!/bin/bash

# Check if .env exists in root directory
if [ -f .env ]; then
    echo "Found .env file in root directory"
else
    # Create .env file from Heroku config
    echo "Creating .env file from Heroku config"
    heroku config -a drive-flow-crm-api > .env
fi

# Make sure .env is included in Docker build
git add .env
git update-index --no-assume-unchanged .env

# Deploy to Heroku
git push heroku main

# Exclude .env from future commits
git update-index --assume-unchanged .env

echo "Deployment complete!" 