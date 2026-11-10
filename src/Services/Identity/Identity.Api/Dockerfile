# syntax=docker/dockerfile:1
# Contexto de build: raiz do repositório.  docker build -f src/Services/Identity/Identity.Api/Dockerfile -t identity-api .
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
# 1) só o que muda pouco: o restore fica em camada própria
COPY global.json Directory.Build.props Directory.Packages.props stylecop.json .editorconfig .globalconfig ./
COPY .config/dotnet-tools.json .config/
COPY src/BuildingBlocks/ConexaoSolidaria.Contracts/*.csproj src/BuildingBlocks/ConexaoSolidaria.Contracts/packages.lock.json src/BuildingBlocks/ConexaoSolidaria.Contracts/
COPY src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/*.csproj src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/packages.lock.json src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/
COPY src/Services/Identity/Identity.Domain/*.csproj src/Services/Identity/Identity.Domain/packages.lock.json src/Services/Identity/Identity.Domain/
COPY src/Services/Identity/Identity.Application/*.csproj src/Services/Identity/Identity.Application/packages.lock.json src/Services/Identity/Identity.Application/
COPY src/Services/Identity/Identity.Infrastructure/*.csproj src/Services/Identity/Identity.Infrastructure/packages.lock.json src/Services/Identity/Identity.Infrastructure/
COPY src/Services/Identity/Identity.Api/*.csproj src/Services/Identity/Identity.Api/packages.lock.json src/Services/Identity/Identity.Api/
RUN dotnet restore src/Services/Identity/Identity.Api/Identity.Api.csproj --locked-mode
# 2) código
COPY src/ src/
RUN dotnet publish src/Services/Identity/Identity.Api/Identity.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Gera o executável de migrations (efbundle) a partir das migrations do EF Core
FROM build AS bundle
# TARGETARCH vem do Docker (amd64 ou arm64): gera o efbundle para a arquitetura da máquina
ARG TARGETARCH
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-arm64 || echo linux-x64) \
    && dotnet tool restore \
    && dotnet ef migrations bundle \
        --project src/Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj \
        --startup-project src/Services/Identity/Identity.Api/Identity.Api.csproj \
        --configuration Release --self-contained --target-runtime "$RID" \
        --output /out/efbundle --force

# Estágio `migrator`: roda as migrations e encerra. A conexão vem de ConnectionStrings__Identity.
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION} AS migrator
WORKDIR /app
COPY --from=bundle /out/efbundle ./efbundle
USER app
ENTRYPOINT ["/bin/sh", "-c", "exec ./efbundle --connection \"$ConnectionStrings__Identity\""]

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER app
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 CMD ["dotnet", "Identity.Api.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
