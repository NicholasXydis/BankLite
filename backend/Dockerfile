FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:306301580fcaa5b445180e759db59309979002d1000669cb4cf58a567d0014bc AS build
WORKDIR /src

COPY BankLite.Application/BankLite.Application.csproj BankLite.Application/
COPY BankLite.Domain/BankLite.Domain.csproj BankLite.Domain/
COPY BankLite.Infrastructure/BankLite.Infrastructure.csproj BankLite.Infrastructure/
COPY BankLiteAPI/BankLite.API/BankLite.API.csproj BankLiteAPI/BankLite.API/
RUN dotnet restore BankLiteAPI/BankLite.API/BankLite.API.csproj

COPY . .
RUN dotnet publish BankLiteAPI/BankLite.API/BankLite.API.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:b0beb9cc1dee1c1b0749796110d4734292071b814207ad0d4f40611f7db04f7b AS final
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
