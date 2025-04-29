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
RUN dotnet publish DriveFlow-CRM-API.csproj -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Simple startup script for environment variables
RUN echo '#!/bin/sh \n\
if [ -f /.env ]; then \n\
  mkdir -p /app/DriveFlow-CRM-API \n\
  cp /.env /app/DriveFlow-CRM-API/.env \n\
fi \n\
dotnet DriveFlow-CRM-API.dll' > /app/start.sh && chmod +x /app/start.sh

# Heroku uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT}

# Run the app
CMD /app/start.sh 