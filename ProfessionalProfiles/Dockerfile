# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION_TAG=v0.0.0
WORKDIR /src

# Copy csproj files
COPY ["ProfessionalProfiles/ProfessionalProfiles.csproj", "ProfessionalProfiles/"]
COPY ["ProfessionalProfiles.Data/ProfessionalProfiles.Data.csproj", "ProfessionalProfiles.Data/"]
COPY ["ProfessionalProfiles.Entities/ProfessionalProfiles.Entities.csproj", "ProfessionalProfiles.Entities/"]
COPY ["ProfessionalProfiles.Shared/ProfessionalProfiles.Shared.csproj", "ProfessionalProfiles.Shared/"]
COPY ["ProfessionalProfiles.Graph/ProfessionalProfiles.Graph.csproj", "ProfessionalProfiles.Graph/"]
COPY ["ProfessionalProfiles.Services/ProfessionalProfiles.Services.csproj", "ProfessionalProfiles.Services/"]

# Restore dependencies
RUN dotnet restore "./ProfessionalProfiles/ProfessionalProfiles.csproj"

# Copy all other files
COPY . .

# Build the project
WORKDIR "/src/ProfessionalProfiles"
RUN dotnet build "./ProfessionalProfiles.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the app
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProfessionalProfiles.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage: runtime container
FROM base AS final
WORKDIR /app

# Runtime environment variable injection for version
ARG VERSION_TAG
ENV VERSION_TAG=${VERSION_TAG}

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ProfessionalProfiles.dll"]