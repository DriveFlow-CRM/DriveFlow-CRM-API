FROM mcr.microsoft.com/dotnet/sdk:8.0.407 AS build
WORKDIR /app

# Copy the whole project first to ensure all files are available
COPY . .

# Setup NuGet properly
RUN mkdir -p /root/.nuget/NuGet
RUN echo '<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>' > /root/.nuget/NuGet/NuGet.Config

# Restore and build the project without trimming
WORKDIR /app/DriveFlow-CRM-API
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out --no-restore -p:PublishSingleFile=false -p:PublishTrimmed=false -p:DebugSymbols=false -p:DebugType=None

# Check build output
RUN ls -la /app/out

# Final stage with runtime only
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Setup environment directories
RUN mkdir -p /app/DriveFlow-CRM-API

# Check final image contents
RUN ls -la /app

# Create a startup script that handles environment and reports errors
RUN echo '#!/bin/bash\n\
echo "Starting application..."\n\
if [ -f /.env ]; then\n\
  echo "Found .env file, copying to application directory"\n\
  cp /.env /app/DriveFlow-CRM-API/.env\n\
fi\n\
echo "Contents of /app:"\n\
ls -la /app\n\
echo "Running with ASPNETCORE_URLS=$ASPNETCORE_URLS"\n\
export ASPNETCORE_ENVIRONMENT=Production\n\
dotnet DriveFlow-CRM-API.dll || { echo "Application failed to start. Error code: $?"; exit 1; }\n\
' > /app/start.sh && chmod +x /app/start.sh

# Configure the port and environment
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE ${PORT}

# Run the application
CMD ["/bin/bash", "/app/start.sh"] 