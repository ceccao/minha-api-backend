# Build a partir da RAIZ do repositorio:
#   docker build -f src/MinhaApi.Api/Dockerfile -t minha-api .

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# .editorconfig entra porque EnforceCodeStyleInBuild faz o build depender dele.
COPY Directory.Build.props .editorconfig ./
COPY src/ ./src/
RUN dotnet restore src/MinhaApi.Api/MinhaApi.Api.csproj
RUN dotnet publish src/MinhaApi.Api/MinhaApi.Api.csproj \
    -c Release -o /app --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app .

# Usuario non-root: a imagem aspnet ja traz o usuario "app" (UID em $APP_UID).
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MinhaApi.Api.dll"]
