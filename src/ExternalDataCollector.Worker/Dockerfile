# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./src/ExternalDataCollector.Domain/ExternalDataCollector.Domain.csproj ./src/ExternalDataCollector.Domain/
COPY ./src/ExternalDataCollector.Application/ExternalDataCollector.Application.csproj ./src/ExternalDataCollector.Application/
COPY ./src/ExternalDataCollector.Infrastructure/ExternalDataCollector.Infrastructure.csproj ./src/ExternalDataCollector.Infrastructure/
COPY ./src/ExternalDataCollector.Worker/ExternalDataCollector.Worker.csproj ./src/ExternalDataCollector.Worker/

RUN dotnet restore ./src/ExternalDataCollector.Worker/ExternalDataCollector.Worker.csproj

COPY . .
RUN dotnet publish ./src/ExternalDataCollector.Worker/ExternalDataCollector.Worker.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExternalDataCollector.Worker.dll"]
