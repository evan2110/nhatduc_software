FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NhatDucSoftware.Core/NhatDucSoftware.Core.csproj NhatDucSoftware.Core/
COPY NhatDucSoftware.Web/NhatDucSoftware.Web.csproj NhatDucSoftware.Web/
RUN dotnet restore NhatDucSoftware.Web/NhatDucSoftware.Web.csproj

COPY NhatDucSoftware.Core/ NhatDucSoftware.Core/
COPY NhatDucSoftware.Web/ NhatDucSoftware.Web/
RUN dotnet publish NhatDucSoftware.Web/NhatDucSoftware.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "NhatDucSoftware.Web.dll"]
