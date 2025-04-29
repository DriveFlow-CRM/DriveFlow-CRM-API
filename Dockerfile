FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
# Copy everything
COPY . .
# Build project
WORKDIR /source/DriveFlow-CRM-API
RUN dotnet restore
RUN dotnet publish -c Release -o /app
# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Create directory for environment files
RUN mkdir -p ./DriveFlow-CRM-API/
# Set environment variables for Heroku compatibility
ENV ASPNETCORE_URLS=http://+:$PORT
# Script to copy environment variables from root to app directory
RUN echo '#!/bin/bash\n\
if [ -f /.env ]; then\n\
  cp /.env ./DriveFlow-CRM-API/\n\
fi\n\
dotnet DriveFlow-CRM-API.dll' > start.sh && chmod +x start.sh
# Required for Heroku
CMD ./start.sh
