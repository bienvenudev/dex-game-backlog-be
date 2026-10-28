# Build stage: the SDK restores, compiles, and publishes the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore before copying the rest, so this layer stays cached until the project file changes.
COPY DexGameBacklog.Api.csproj ./
RUN dotnet restore DexGameBacklog.Api.csproj

COPY . ./
RUN dotnet publish DexGameBacklog.Api.csproj -c Release -o /app --no-restore

# Runtime stage: only the ASP.NET runtime and the published output, no SDK or source.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

# The image listens on 8080 and ships a non-root user; run as that user.
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DexGameBacklog.Api.dll"]
