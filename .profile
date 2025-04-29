#!/bin/bash

# Copy environment variables to the correct location
if [ -f ".env" ]; then
    echo "Copying environment variables..."
    cp -f .env ./DriveFlow-CRM-API/.env
fi 