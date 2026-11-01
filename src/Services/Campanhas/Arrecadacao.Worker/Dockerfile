# syntax=docker/dockerfile:1
# Contexto de build: raiz do repositório.  docker build -f src/Services/Campanhas/Arrecadacao.Worker/Dockerfile -t arrecadacao-worker .
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
# 1) só o que muda pouco: o restore fica em camada própria
COPY global.json Directory.Build.props Directory.Packages.props stylecop.json .editorconfig .globalconfig ./
COPY src/BuildingBlocks/ConexaoSolidaria.Contracts/*.csproj src/BuildingBlocks/ConexaoSolidaria.Contracts/packages.lock.json src/BuildingBlocks/ConexaoSolidaria.Contracts/
COPY src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/*.csproj src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/packages.lock.json src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/
COPY src/Services/Campanhas/Campanhas.Domain/*.csproj src/Services/Campanhas/Campanhas.Domain/packages.lock.json src/Services/Campanhas/Campanhas.Domain/
COPY src/Services/Campanhas/Campanhas.Application/*.csproj src/Services/Campanhas/Campanhas.Application/packages.lock.json src/Services/Campanhas/Campanhas.Application/
COPY src/Services/Campanhas/Campanhas.Infrastructure/*.csproj src/Services/Campanhas/Campanhas.Infrastructure/packages.lock.json src/Services/Campanhas/Campanhas.Infrastructure/
COPY src/Services/Campanhas/Arrecadacao.Worker/*.csproj src/Services/Campanhas/Arrecadacao.Worker/packages.lock.json src/Services/Campanhas/Arrecadacao.Worker/
RUN dotnet restore src/Services/Campanhas/Arrecadacao.Worker/Arrecadacao.Worker.csproj --locked-mode
# 2) código
COPY src/ src/
RUN dotnet publish src/Services/Campanhas/Arrecadacao.Worker/Arrecadacao.Worker.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
# O Worker serve só as sondas de saúde e as métricas, na porta 9090.
ENV ASPNETCORE_URLS=http://+:9090 DOTNET_EnableDiagnostics=0
EXPOSE 9090
COPY --from=build /app/publish .
USER app
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 CMD ["dotnet", "Arrecadacao.Worker.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "Arrecadacao.Worker.dll"]
