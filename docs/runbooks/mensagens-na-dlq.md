# Runbook — Mensagens na fila de erro (DLQ)

[← Runbooks](README.md)

| Item | Valor |
|---|---|
| Alerta | `MensagensNaDLQ` |
| Condição | Alguma fila `*_error` com mensagens por 1 minuto |
| Severidade | Crítica |

## Sintoma

Uma mensagem falhou em todas as retentativas. Filas possíveis: `doacao-recebida_error` (doação não somada), `notificacoes-recibo_error` (recibo não emitido), `indexar-campanha_error` (busca desatualizada).

## Impacto

Na fila de doação, o total da campanha fica menor que o real até o reprocessamento. Nenhuma doação é perdida: ela está na fila de erro.

## Diagnóstico

1. Abrir o painel do RabbitMQ:
   ```bash
   kubectl port-forward -n infra svc/rabbitmq 15672:15672
   ```
   Acessar http://localhost:15672 → Queues → fila `*_error` → **Get messages** (modo *Nack message requeue true*, para só olhar).
2. Ler os headers `MT-Fault-Message`, `MT-Fault-ExceptionType` e `MT-Fault-StackTrace`.
3. Procurar o `traceparent` da mensagem no Jaeger e os logs do consumidor:
   ```bash
   kubectl logs -n conexao deploy/arrecadacao-worker --since=1h | grep <doacaoId>
   ```

## Resolução

1. Corrigir a causa (dado inválido, defeito no consumidor ou dependência fora do ar).
2. Reenviar as mensagens para a fila original, com confirmação de outro integrante:
   - RabbitMQ → Queues → fila `*_error` → **Move messages** → destino `doacao-recebida` (ou a fila original).
3. Conferir o resultado: a fila de erro zera e, para doações, o total no painel aumenta.

O reprocessamento é seguro: o Worker é idempotente por `doacaoId` ([ADR 0006](../adr/0006-consumidores-idempotentes.md)).

## Prevenção

Teste de regressão com a mensagem que falhou (anonimizada) e revisão da política de retentativas.
