FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app
# Install Git command-line for DD repository operations
RUN apt-get update && apt-get install -y git-core
ENV GIT_PATH=/usr/bin/git
FROM base AS final
COPY app/ /app/
WORKDIR /app
ENTRYPOINT ["dotnet", "DataDock.Worker.dll"]
