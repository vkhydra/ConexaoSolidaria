# Runbooks

[← README](../../README.md)

Procedimentos de resposta para cada alerta do Prometheus ([observabilidade](../../specs/product/observabilidade.md)) e para operações sensíveis. Cada alerta aponta para o seu runbook pela anotação `runbook_url`.

| Runbook | Tipo | Severidade |
|---|---|---|
| [Taxa de erros 5xx alta](taxa-de-erros-5xx-alta.md) | Alerta `TaxaDeErros5xxAlta` | Crítica |
| [Latência do painel alta](latencia-painel-alta.md) | Alerta `LatenciaPainelAlta` | Aviso |
| [Mensagens na DLQ](mensagens-na-dlq.md) | Alerta `MensagensNaDLQ` | Crítica |
| [Fila de doações acumulando](fila-doacoes-acumulando.md) | Alerta `FilaDoacoesAcumulando` | Aviso |
| [Pod reiniciando](pod-reiniciando.md) | Alerta `PodReiniciando` | Aviso |
| [Serviço sem réplicas](servico-sem-replicas.md) | Alerta `ServicoSemReplicas` | Crítica |
| [Rotação da chave do JWT](rotacao-chave-jwt.md) | Operação | — |

## Modelo

Todo runbook tem: **Sintoma**, **Impacto**, **Diagnóstico** (comandos), **Resolução**, **Prevenção**. Os comandos usam `kubectl` e funcionam em `pwsh`, `bash` e `zsh`.

## Regras

- Nunca colar segredos, tokens ou dados pessoais em canais de comunicação durante o incidente.
- Comandos destrutivos (`kubectl delete`, reenvio em massa de mensagens) só com confirmação de outro integrante.
- Depois de resolver, registrar o que aconteceu e, se for o caso, criar issue para a prevenção.
