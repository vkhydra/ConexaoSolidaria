# syntax=docker/dockerfile:1
# Contexto de build: raiz do repositório.  docker build -f src/Services/Campanhas/Campanhas.Api/Dockerfile -t campanhas-api .
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
# 1) só o que muda pouco: o restore fica em camada própria
COPY global.json Directory.Build.props Directory.Packages.props stylecop.json .editorconfig .globalconfig ./
COPY .config/dotnet-tools.json .config/
COPY src/BuildingBlocks/ConexaoSolidaria.Contracts/*.csproj src/BuildingBlocks/ConexaoSolidaria.Contracts/packages.lock.json src/BuildingBlocks/ConexaoSolidaria.Contracts/
COPY src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/*.csproj src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/packages.lock.json src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/
COPY src/Services/Campanhas/Campanhas.Domain/*.csproj src/Services/Campanhas/Campanhas.Domain/packages.lock.json src/Services/Campanhas/Campanhas.Domain/
COPY src/Services/Campanhas/Campanhas.Application/*.csproj src/Services/Campanhas/Campanhas.Application/packages.lock.json src/Services/Campanhas/Campanhas.Application/
COPY src/Services/Campanhas/Campanhas.Infrastructure/*.csproj src/Services/Campanhas/Campanhas.Infrastructure/packages.lock.json src/Services/Campanhas/Campanhas.Infrastructure/
COPY src/Services/Campanhas/Campanhas.Api/*.csproj src/Services/Campanhas/Campanhas.Api/packages.lock.json src/Services/Campanhas/Campanhas.Api/
RUN dotnet restore src/Services/Campanhas/Campanhas.Api/Campanhas.Api.csproj --locked-mode
# 2) código
COPY src/ src/
RUN dotnet publish src/Services/Campanhas/Campanhas.Api/Campanhas.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Gera o executável de migrations (efbundle) a partir das migrations do EF Core
FROM build AS bundle
# TARGETARCH vem do Docker (amd64 ou arm64): gera o efbundle para a arquitetura da máquina
ARG TARGETARCH
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-arm64 || echo linux-x64) \
    && dotnet tool restore \
    && dotnet ef migrations bundle \
        --project src/Services/Campanhas/Campanhas.Infrastructure/Campanhas.Infrastructure.csproj \
        --startup-project src/Services/Campanhas/Campanhas.Api/Campanhas.Api.csproj \
        --configuration Release --self-contained --target-runtime "$RID" \
        --output /out/efbundle --force

# Estágio `migrator`: roda as migrations e encerra. A conexão vem de ConnectionStrings__Campanhas.
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION} AS migrator
WORKDIR /app
COPY --from=bundle /out/efbundle ./efbundle
USER app
ENTRYPOINT ["/bin/sh", "-c", "exec ./efbundle --connection \"$ConnectionStrings__Campanhas\""]

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER app
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 CMD ["dotnet", "Campanhas.Api.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "Campanhas.Api.dll"]
