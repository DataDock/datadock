FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
# install nodejs for angular, webpack middleware
RUN apt-get -qq update && apt-get -qqy --no-install-recommends install wget gnupg \
    git \
    unzip
RUN curl -sL https://deb.nodesource.com/setup_6.x |  bash -
RUN apt-get install -y nodejs
COPY app/ /app/
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "DataDock.Web.dll"]
