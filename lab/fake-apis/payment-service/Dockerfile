FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore && dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5020
ENV ASPNETCORE_URLS=http://+:5020
ENTRYPOINT ["dotnet", "PaymentService.dll"]
