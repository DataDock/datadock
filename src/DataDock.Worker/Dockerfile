FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app
# Install Git command-line for DD repository operations
RUN apt-get update && apt-get install -y git-core
ENV GIT_PATH=/usr/bin/git

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY . .
RUN dotnet restore DataDock.sln
WORKDIR /src/DataDock.Worker
RUN dotnet build -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DataDock.Worker.dll"]
