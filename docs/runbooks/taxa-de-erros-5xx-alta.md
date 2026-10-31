# Runbook — Taxa de erros 5xx alta

[← Runbooks](README.md)

| Item | Valor |
|---|---|
| Alerta | `TaxaDeErros5xxAlta` |
| Condição | Mais de 5% das respostas de um serviço com status 5xx por 5 minutos |
| Severidade | Crítica |

## Sintoma

Usuários recebem erro ao usar a plataforma. No Grafana, o painel "Erros (% de 5xx)" do serviço está acima de 5%.

## Impacto

Funcionalidade do serviço afetado indisponível ou instável (login, gestão, painel ou doação).

## Diagnóstico

1. Identificar o serviço no rótulo `job` do alerta.
2. Ver a prontidão das dependências:
   ```bash
   kubectl get pods -n conexao -l app.kubernetes.io/name=<servico>
   kubectl port-forward -n conexao deploy/<servico> 8080:8080
   curl http://localhost:8080/health/ready
   ```
3. Ver os erros recentes com `traceId`:
   ```bash
   kubectl logs -n conexao deploy/<servico> --since=15m | grep '"@l":"Error"'
   ```
4. Abrir o `traceId` no Jaeger e localizar o span com erro.
5. Conferir se houve deploy recente: `kubectl rollout history deploy/<servico> -n conexao`.

## Resolução

| Causa | Ação |
|---|---|
| Dependência fora do ar (banco, RabbitMQ, Redis) | Seguir [serviço sem réplicas](servico-sem-replicas.md) para o componente de infraestrutura |
| Versão nova com defeito | `kubectl rollout undo deploy/<servico> -n conexao` e abrir issue com o `traceId` |
| Esgotamento de recursos | Ver [pod reiniciando](pod-reiniciando.md) |

## Prevenção

Teste de regressão para o defeito encontrado (constituição, princípio III) e revisão dos limites de recursos.
