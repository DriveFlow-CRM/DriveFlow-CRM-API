FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY DriveFlow-CRM-API/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY DriveFlow-CRM-API/. ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Copy environment file if exists and set up a startup script
RUN echo '#!/bin/bash \n\
if [ -f /.env ]; then \n\
  mkdir -p ./DriveFlow-CRM-API \n\
  cp /.env ./DriveFlow-CRM-API/.env \n\
fi \n\
exec dotnet DriveFlow-CRM-API.dll' > /app/start.sh && \
chmod +x /app/start.sh

ENV PORT=8080
EXPOSE 8080

# Set environment variables for Heroku compatibility
ENV ASPNETCORE_URLS=http://+:${PORT}

CMD /app/start.sh 