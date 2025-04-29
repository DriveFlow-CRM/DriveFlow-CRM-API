FROM mcr.microsoft.com/dotnet/sdk:8.0.407 AS build
WORKDIR /source

# Copy only the API project
COPY DriveFlow-CRM-API/ ./

# Configure NuGet properly
RUN mkdir -p /root/.nuget/NuGet && \
    echo '<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>' > /root/.nuget/NuGet/NuGet.Config

# Build the project directly without using the solution file
RUN dotnet restore DriveFlow-CRM-API.csproj
RUN dotnet publish DriveFlow-CRM-API.csproj -c Release -o /app -p:PublishTrimmed=false -p:PublishSingleFile=false -p:TreatWarningsAsErrors=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Create startup script
RUN echo '#!/bin/bash\necho "Starting application..."\nif [ -f /.env ]; then\n  echo "Found .env file, copying to application directory"\n  cp /.env /app/.env\nfi\nexport ASPNETCORE_ENVIRONMENT=Production\nexec dotnet DriveFlow-CRM-API.dll' > /app/start.sh && chmod +x /app/start.sh

# Configure for Heroku
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}

# Run the app
CMD ["/app/start.sh"] 