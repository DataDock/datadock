FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
COPY app/ /app/
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "DataDock.Web.dll"]
