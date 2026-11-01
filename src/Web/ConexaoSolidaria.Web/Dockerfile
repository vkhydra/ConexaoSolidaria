# syntax=docker/dockerfile:1
# Contexto de build: raiz do repositório.  docker build -f src/Web/ConexaoSolidaria.Web/Dockerfile -t web .
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
# 1) só o que muda pouco: o restore fica em camada própria
COPY global.json Directory.Build.props Directory.Packages.props stylecop.json .editorconfig .globalconfig ./
COPY src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/*.csproj src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/packages.lock.json src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/
# O SDK Web (Razor) inclui o pacote de ativos internos só quando vê os componentes e o wwwroot; por isso o projeto inteiro entra antes do restore.
COPY src/Web/ConexaoSolidaria.Web/ src/Web/ConexaoSolidaria.Web/
RUN dotnet restore src/Web/ConexaoSolidaria.Web/ConexaoSolidaria.Web.csproj --locked-mode
# 2) código dos demais projetos
COPY src/ src/
RUN dotnet publish src/Web/ConexaoSolidaria.Web/ConexaoSolidaria.Web.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER app
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 CMD ["dotnet", "ConexaoSolidaria.Web.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "ConexaoSolidaria.Web.dll"]
