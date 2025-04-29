FROM mcr.microsoft.com/dotnet/sdk:8.0.407 AS build
WORKDIR /app

# Copy just what's needed for build
COPY DriveFlow-CRM-API/*.csproj ./
# Fix NuGet package resolution by clearing NuGet.Config and using Linux paths
RUN mkdir -p /root/.nuget/NuGet
RUN echo '<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>' > /root/.nuget/NuGet/NuGet.Config
# Restore dependencies
RUN dotnet restore --force

# Copy and publish only the project file
COPY DriveFlow-CRM-API/. ./

# Disable trimming since it's causing problems
RUN dotnet publish DriveFlow-CRM-API.csproj -c Release -o /app/publish --no-restore -p:PublishTrimmed=false -p:SelfContained=false

# Check what was built
RUN ls -la /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# List all files to debug
RUN ls -la /app

# Simple startup script that runs our application and copies env vars
RUN echo '#!/bin/sh \n\
mkdir -p /app/DriveFlow-CRM-API \n\
if [ -f /.env ]; then \n\
  cp /.env /app/DriveFlow-CRM-API/.env \n\
fi \n\
ls -la \n\
dotnet DriveFlow-CRM-API.dll' > /app/start.sh && chmod +x /app/start.sh

# Heroku uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT}

# Run the app
CMD /app/start.sh 