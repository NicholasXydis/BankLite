FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:c0790639332692a0d56cdd81ed581cfd24d040d9839764c138994866df89a3b6 AS build
WORKDIR /src

COPY BankLite.Application/BankLite.Application.csproj BankLite.Application/
COPY BankLite.Domain/BankLite.Domain.csproj BankLite.Domain/
COPY BankLite.Infrastructure/BankLite.Infrastructure.csproj BankLite.Infrastructure/
COPY BankLiteAPI/BankLite.API/BankLite.API.csproj BankLiteAPI/BankLite.API/
RUN dotnet restore BankLiteAPI/BankLite.API/BankLite.API.csproj

COPY . .
RUN dotnet publish BankLiteAPI/BankLite.API/BankLite.API.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:19be23fe71e885186495d8da1f2d417e553daaa99e16e5148c5ca3ee2f062512 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get upgrade --yes \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

COPY --chown=$APP_UID:$APP_UID --from=build /app/publish .

USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "BankLite.API.dll"]
