# Build stage: the SDK restores, compiles, and publishes the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore before copying the rest, so this layer stays cached until the project file changes.
COPY DexGameBacklog.Api.csproj ./
RUN dotnet restore DexGameBacklog.Api.csproj

COPY . ./
RUN dotnet publish DexGameBacklog.Api.csproj -c Release -o /app --no-restore

# Compile the migrations into one executable that the Kubernetes Job runs.
# The tool lives only in this build stage; the version matches the EF packages.
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="$PATH:/root/.dotnet/tools"
# Program.cs requires a connection string to build the DbContext. The bundle never
# connects at build time, so a placeholder for this command is enough; the Job
# passes the real one with --connection.
RUN ConnectionStrings__DefaultConnection="Host=placeholder" \
    dotnet ef migrations bundle --project DexGameBacklog.Api.csproj -o /app/efbundle

# Runtime stage: only the ASP.NET runtime and the published output, no SDK or source.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

# The image listens on 8080 and ships a non-root user; run as that user.
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DexGameBacklog.Api.dll"]
