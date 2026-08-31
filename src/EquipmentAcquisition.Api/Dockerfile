# Build context is the repo root (see docker-compose.yml) — paths below are
# relative to there, not to this file's own directory.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/EquipmentAcquisition.Api/ src/EquipmentAcquisition.Api/
COPY src/EquipmentAcquisition.Core/ src/EquipmentAcquisition.Core/
COPY src/EquipmentAcquisition.Domain/ src/EquipmentAcquisition.Domain/

WORKDIR /src/src/EquipmentAcquisition.Api
# Restore and publish in one step (not a separate `dotnet restore` + `--no-restore`
# publish) — the split reliably failed here with NETSDK1064 on an implicit analyzer
# package for this multi-project-reference solution, even with a clean cache;
# combining them sidesteps whatever inconsistency caused it.
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
# CMD, not ENTRYPOINT — so `docker compose run api dotnet EquipmentAcquisition.Api.dll --seed`
# (see docker-deployment.md) replaces this cleanly instead of appending to it.
CMD ["dotnet", "EquipmentAcquisition.Api.dll"]
