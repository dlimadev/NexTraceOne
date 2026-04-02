FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore && dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5030
ENV ASPNETCORE_URLS=http://+:5030
ENTRYPOINT ["dotnet", "InventoryService.dll"]
