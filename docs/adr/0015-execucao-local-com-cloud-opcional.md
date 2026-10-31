# ADR 0015 — Execução local com cloud opcional na Azure

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

O Hackathon pede Kubernetes local. As Fases 3 e 4 trabalharam com cloud, que gera custo.

## Decisão

Ambiente oficial é o Minikube. A Etapa 15 (opcional) implanta na Azure (AKS, ACR, Key Vault) com Bicep, e a infraestrutura é apagada depois da gravação.

## Alternativas consideradas

AWS (EKS, ECR); cloud obrigatória.

## Consequências

Sem custo obrigatório; overlays de Kustomize separam local e Azure.

## Referências

- [Infraestrutura](../../specs/product/infraestrutura.md)
- [Manual de implementação](../manual-de-implementacao.md)
