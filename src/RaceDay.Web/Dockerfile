# Build context is the REPO ROOT (see docker-compose.yml's build.context).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/RaceDay.Web/RaceDay.Web.csproj", "src/RaceDay.Web/"]
RUN dotnet restore "src/RaceDay.Web/RaceDay.Web.csproj"

COPY ["src/RaceDay.Web/", "src/RaceDay.Web/"]
WORKDIR /src/src/RaceDay.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Data Protection keys (see Program.cs's DataProtection:KeysPath) live on
# a mounted volume at /keys so a restart doesn't silently invalidate
# every logged-in user's .RaceDay.Web.Session cookie.
RUN mkdir -p /keys && chown $APP_UID:$APP_UID /keys

USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
