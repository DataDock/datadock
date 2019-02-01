FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
RUN apt-get -qq update && apt-get -qqy --no-install-recommends install wget gnupg \
    git \
    unzip
RUN curl -sL https://deb.nodesource.com/setup_10.x |  bash -
RUN apt-get install -y nodejs
WORKDIR /src
COPY DataDock.Web DataDock.Web/
COPY DataDock.Common DataDock.Common/
COPY DataDock.sln .
RUN dotnet restore DataDock.Web
WORKDIR /src/DataDock.Web
RUN npm rebuild node-sass --force
RUN dotnet build -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DataDock.Web.dll"]
