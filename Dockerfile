# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish src/Api/StaqFinance.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-self-contained \
    -p:UseAppHost=false

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Remove development-only settings from the image
RUN rm -f appsettings.Development.json

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "StaqFinance.Api.dll"]
