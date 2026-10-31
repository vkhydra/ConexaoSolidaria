# ADR 0010 — Notificações como função serverless com KEDA

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

a Fase 3 pede funções serverless acionadas por eventos. O ambiente principal é local.

## Decisão

`Notificacoes.Function` em Azure Functions isolated worker com gatilho RabbitMQ, empacotada como container e escalada de 0 a 3 réplicas pelo KEDA. Na cloud opcional, a mesma imagem roda no AKS com KEDA.

## Alternativas consideradas

AWS Lambda com LocalStack (exigiria ponte RabbitMQ → SQS); Function no plano Consumption da Azure (gatilho RabbitMQ não é suportado nesse plano).

## Consequências

modelo de programação serverless e escala a zero, rodando localmente. Se o runtime do Functions não suportar .NET 10, a função usa .NET 8 isolated.

## Referências

- [Feature 007 — Recibo](../../specs/features/007-recibo-doacao/plan.md)
- [Infraestrutura](../../specs/product/infraestrutura.md)
