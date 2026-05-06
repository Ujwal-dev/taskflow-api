# ─────────────────────────────────────────────────────────────────────────────
# Stage 1 — RESTORE
# Pull in dependencies first so Docker can cache this layer.
# If only source files change (not .csproj), this layer is reused — faster builds.
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY TaskFlowAPI.csproj .
RUN dotnet restore TaskFlowAPI.csproj

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2 — BUILD
# Copy source and build in Release mode.
# ─────────────────────────────────────────────────────────────────────────────
FROM restore AS build
COPY . .
RUN dotnet build TaskFlowAPI.csproj \
    --configuration Release \
    --no-restore \
    --output /app/build

# ─────────────────────────────────────────────────────────────────────────────
# Stage 3 — PUBLISH
# Publish a self-contained, trimmed output ready for the runtime image.
# ─────────────────────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish TaskFlowAPI.csproj \
    --configuration Release \
    --no-build \
    --output /app/publish \
    /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────────────
# Stage 4 — RUNTIME (final image)
# Uses the much smaller ASP.NET runtime image — NOT the full SDK.
# SDK image ~900MB vs runtime image ~220MB — this is what gets deployed.
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Run as non-root user for security — never run production containers as root
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

WORKDIR /app

# Copy only the published output from Stage 3 — no SDK, no source code
COPY --from=publish /app/publish .

# Switch to non-root user
USER appuser

# Port 8080 is the default for Azure App Service containers
EXPOSE 8080

# Set ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check — Azure App Service uses this to know the container is ready
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TaskFlowAPI.dll"]