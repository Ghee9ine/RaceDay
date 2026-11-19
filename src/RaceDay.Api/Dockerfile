# Build context is the REPO ROOT (see docker-compose.yml's build.context),
# not this folder, so the API's own ProjectReference-free build can still
# copy just the files it needs without copying the whole repo into the
# build stage.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/RaceDay.Api/RaceDay.Api.csproj", "src/RaceDay.Api/"]
RUN dotnet restore "src/RaceDay.Api/RaceDay.Api.csproj"

COPY ["src/RaceDay.Api/", "src/RaceDay.Api/"]
WORKDIR /src/src/RaceDay.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl is only here so HEALTHCHECK below has something to call /health
# with - the runtime image doesn't ship it by default.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Data Protection keys (see Program.cs's DataProtection:KeysPath) live on
# a mounted volume at /keys so they survive a container restart - created
# and chowned here, while still root, since the non-root user below has
# no permission to create it itself.
RUN mkdir -p /keys && chown $APP_UID:$APP_UID /keys

# Run as the non-root user the base image already provides, rather than
# the default root - standard container hardening, not RaceDay-specific.
USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
