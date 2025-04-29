#!/bin/bash

# Script to create .env file in the DriveFlow-CRM-API directory from Heroku config
echo "Fetching environment variables from Heroku..."
heroku config -a drive-flow-crm-api > .env.temp

# Process the file to remove header
tail -n +2 .env.temp > DriveFlow-CRM-API/.env
rm .env.temp

echo "Environment file created in DriveFlow-CRM-API/.env"
echo "Ready for deployment!" 