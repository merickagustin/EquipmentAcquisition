# Build context is the repo root (see docker-compose.yml).
# Two independent build stages — the client build has no knowledge of the
# .NET project layout, and vice versa. See architecture.md's "Output goes to
# client/dist, not directly into wwwroot" note for why this stays decoupled.

FROM node:20-alpine AS client-build
WORKDIR /client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/EquipmentAcquisition.Web/EquipmentAcquisition.Web.csproj", "src/EquipmentAcquisition.Web/"]
RUN dotnet restore "src/EquipmentAcquisition.Web/EquipmentAcquisition.Web.csproj"

COPY src/EquipmentAcquisition.Web/ src/EquipmentAcquisition.Web/

WORKDIR /src/src/EquipmentAcquisition.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# The Dockerfile is the "separate step" architecture.md mentions — copies the
# client build's output into wwwroot/dist, where the app's own UseStaticFiles()
# already serves from in non-Development (see Program.cs).
COPY --from=client-build /client/dist ./wwwroot/dist
EXPOSE 8080
CMD ["dotnet", "EquipmentAcquisition.Web.dll"]
