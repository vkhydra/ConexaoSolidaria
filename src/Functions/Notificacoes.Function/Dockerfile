# syntax=docker/dockerfile:1
# Contexto de build: raiz do repositório.  docker build -f src/Functions/Notificacoes.Function/Dockerfile -t notificacoes-function .
# Imagem base de Functions isolated: traz o host (func) e carrega a extensão do gatilho RabbitMQ gerada no publish.
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props stylecop.json .editorconfig .globalconfig ./
COPY src/BuildingBlocks/ConexaoSolidaria.Contracts/*.csproj src/BuildingBlocks/ConexaoSolidaria.Contracts/packages.lock.json src/BuildingBlocks/ConexaoSolidaria.Contracts/
COPY src/Functions/Notificacoes.Function/*.csproj src/Functions/Notificacoes.Function/packages.lock.json src/Functions/Notificacoes.Function/
RUN dotnet restore src/Functions/Notificacoes.Function/Notificacoes.Function.csproj --locked-mode
COPY src/ src/
RUN dotnet publish src/Functions/Notificacoes.Function/Notificacoes.Function.csproj \
    -c Release -o /home/site/wwwroot --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS final
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
COPY --from=build /home/site/wwwroot /home/site/wwwroot
