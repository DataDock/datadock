FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app
# Install Git command-line for DD repository operations
RUN apt-get update && apt-get install -y git-core
ENV GIT_PATH=/usr/bin/git
FROM base AS final
COPY app/ /app/
WORKDIR /app
ENTRYPOINT ["dotnet", "DataDock.Worker.dll"]
