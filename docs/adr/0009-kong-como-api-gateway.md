# ADR 0009 — Kong como API Gateway

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

bônus do enunciado; conteúdo da Fase 3 (Kong e Azure APIM).

## Decisão

Kong Ingress Controller em modo DB-less, configurado por recursos Kubernetes (`Ingress`, `KongPlugin`) versionados.

## Alternativas consideradas

Ocelot ou YARP (código .NET; não é conteúdo do curso); Azure APIM (pago, só na cloud); NGINX Ingress (sem plugins de API).

## Consequências

rate limiting, CORS, correlation-id e métricas sem código; a Admin API nunca é exposta.

## Referências

- [Infraestrutura](../../specs/product/infraestrutura.md)
