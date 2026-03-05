# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./src/ExternalDataCollector.Domain/ExternalDataCollector.Domain.csproj ./src/ExternalDataCollector.Domain/
COPY ./src/ExternalDataCollector.Application/ExternalDataCollector.Application.csproj ./src/ExternalDataCollector.Application/
COPY ./src/ExternalDataCollector.Infrastructure/ExternalDataCollector.Infrastructure.csproj ./src/ExternalDataCollector.Infrastructure/
COPY ./src/ExternalDataCollector.Api/ExternalDataCollector.Api.csproj ./src/ExternalDataCollector.Api/

RUN dotnet restore ./src/ExternalDataCollector.Api/ExternalDataCollector.Api.csproj

COPY . .
RUN dotnet publish ./src/ExternalDataCollector.Api/ExternalDataCollector.Api.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ExternalDataCollector.Api.dll"]
