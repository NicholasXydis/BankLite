FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:f7e28a0a3f6f9235929ec19274fd0ddf8534e9159f99fac602ac73304f1ce99f AS build
WORKDIR /src

COPY BankLite.Application/BankLite.Application.csproj BankLite.Application/
COPY BankLite.Domain/BankLite.Domain.csproj BankLite.Domain/
COPY BankLite.Infrastructure/BankLite.Infrastructure.csproj BankLite.Infrastructure/
COPY BankLiteAPI/BankLite.API/BankLite.API.csproj BankLiteAPI/BankLite.API/
RUN dotnet restore BankLiteAPI/BankLite.API/BankLite.API.csproj

COPY . .
RUN dotnet publish BankLiteAPI/BankLite.API/BankLite.API.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:1601380775673021a23ea5985669f1daf03719faaa684f839da1752fb48cfc7f AS final
WORKDIR /app

RUN apt-get update \
    && apt-get upgrade --yes \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

COPY --chown=$APP_UID:$APP_UID --from=build /app/publish .

USER $APP_UID

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 CMD curl --fail --silent --show-error http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "BankLite.API.dll"]
