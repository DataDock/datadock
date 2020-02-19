FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
COPY app/ /app/
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "DataDock.Web.dll"]
