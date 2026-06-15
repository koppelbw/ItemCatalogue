# syntax=docker/dockerfile:1

# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: BUILD — uses the full .NET SDK image to restore + publish the app.
# This image is large (~800MB) but it never ships; we only copy its output.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy ONLY the .csproj files first, then restore. Docker caches each layer, so as
# long as no .csproj changes, the (slow) restore layer is reused — even when you
# edit C# source. This is the classic Docker build-cache optimisation.
COPY Domain/Domain.csproj                     Domain/
COPY Application/Application.csproj            Application/
COPY Persistence/Persistence.csproj           Persistence/
COPY ItemCatalogueAPI/ItemCatalogueAPI.csproj ItemCatalogueAPI/
RUN dotnet restore ItemCatalogueAPI/ItemCatalogueAPI.csproj

# Now copy the actual source for the API and the projects it references, and
# publish a Release build. --no-restore because we just restored above.
COPY Domain/          Domain/
COPY Application/     Application/
COPY Persistence/     Persistence/
COPY ItemCatalogueAPI/ ItemCatalogueAPI/
RUN dotnet publish ItemCatalogueAPI/ItemCatalogueAPI.csproj \
    -c Release -o /app/publish --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: RUNTIME — small ASP.NET image with just the runtime, no SDK/compilers.
# Only the published output from stage 1 is copied in, keeping the image lean.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The .NET base image defaults Kestrel to port 8080 inside the container.
EXPOSE 8080
ENTRYPOINT ["dotnet", "ItemCatalogueAPI.dll"]
