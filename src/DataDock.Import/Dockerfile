FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY DataDock.Import/DataDock.Import.csproj DataDock.Import/
COPY DataDock.Common/DataDock.Common.csproj DataDock.Common/
RUN dotnet restore DataDock.Import/DataDock.Import.csproj
COPY . .
WORKDIR /src/DataDock.Import
RUN dotnet build DataDock.Import.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish DataDock.Import.csproj -c Release -o /app

FROM base AS final
WORKDIR /data
COPY DataDock.Import/data .
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DataDock.Import.dll"]
