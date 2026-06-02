FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:fc69dc5e0c9789adaac5c8efce71ead4d016a51318667c4f26ce93574b1b9403 AS build
WORKDIR /src

COPY BankLite.Application/BankLite.Application.csproj BankLite.Application/
COPY BankLite.Domain/BankLite.Domain.csproj BankLite.Domain/
COPY BankLite.Infrastructure/BankLite.Infrastructure.csproj BankLite.Infrastructure/
COPY BankLiteAPI/BankLite.API/BankLite.API.csproj BankLiteAPI/BankLite.API/
RUN dotnet restore BankLiteAPI/BankLite.API/BankLite.API.csproj

COPY . .
RUN dotnet publish BankLiteAPI/BankLite.API/BankLite.API.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:8c0b6857eab7b2aa57884c839bf4678414606bd7d17370f18a842ac5cf414711 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get upgrade --yes \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

COPY --chown=$APP_UID:$APP_UID --from=build /app/publish .

USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "BankLite.API.dll"]
