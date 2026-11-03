# Justificativa dos bancos de dados

[← README](../README.md)

A Conexão Solidária usa **persistência poliglota** ([ADR 0007](adr/0007-persistencia-poliglota.md)): cada banco resolve o padrão de acesso para o qual é mais forte. O modelo de tabelas e coleções está em [specs/product/dados.md](../specs/product/dados.md).

## Resumo

| Banco | Dono | Papel | Consistência | Por que este e não outro |
|---|---|---|---|---|
| PostgreSQL 17 (`identity_db`) | Identity | Usuários, consentimento, tentativas de login | ACID | Unicidade de e-mail e CPF por constraint; transações |
| PostgreSQL 17 (`campanhas_db`) | Campanhas | Campanhas, total arrecadado, idempotência, Outbox | ACID | `numeric(18,2)` exato para dinheiro; Outbox na mesma transação dos dados |
| MongoDB 8 | Worker | Livro de doações (histórico imutável) | Forte no primário | Escrita só de acréscimo, documento autocontido, agregação por campanha |
| Redis 8 | Campanhas e Worker | Cache do painel de transparência e deduplicação do recibo | Cache descartável | Leitura em memória (< 1 ms), TTL e remoção pontual |
| Elasticsearch 9 | Campanhas | Índice de busca de campanhas | Quase em tempo real (~1 s) | Tolerância a erro de digitação, analisador em português, relevância |

## Por necessidade

### Valores financeiros e unicidade: PostgreSQL

- Dinheiro em `numeric(18,2)`, sem erro de arredondamento de ponto flutuante.
- Unicidade de e-mail e de hash do CPF garantida pelo banco, mesmo sob concorrência.
- O Transactional Outbox grava o evento na **mesma transação** da mudança de estado, o que evita doação registrada sem evento (e o contrário).
- **Alternativa descartada:** NoSQL para esses dados. Não oferece a mesma garantia transacional com o Outbox do EF Core.

### Histórico imutável de doações: MongoDB

- O livro de doações só recebe inserções e é lido por agregação, sem junções.
- Cada doação é um documento autocontido (campanha, valor, instante), o que combina com o modelo de documentos.
- Fica fora do PostgreSQL para a escrita do histórico não competir com a carga transacional.
- **Alternativa descartada:** Elasticsearch como fonte da verdade, pois ele é um índice que pode ser reconstruído e não um registro durável.

### Painel público muito lido: Redis

- O painel de transparência é público e consultado com frequência; a resposta vem da memória.
- O Worker remove a chave da campanha quando soma uma doação, então o painel nunca fica mais velho que o TTL.
- Se o Redis cair, o painel volta a ler do PostgreSQL (degradação, não indisponibilidade).
- **Alternativa descartada:** consultar o banco a cada acesso, o que aumenta latência e carga sem ganho de consistência.

### Busca tolerante a erros: Elasticsearch

- Índice invertido com analisador em português, busca `fuzzy` e relevância por `_score`.
- O índice é derivado dos dados de campanhas e pode ser recriado pelo endpoint administrativo de reindexação.
- **Alternativa descartada:** `LIKE` / `ILIKE` no PostgreSQL, que não tolera erro de digitação nem ordena por relevância.

## Isolamento entre serviços

Cada bounded context tem o seu banco e as suas credenciais: o Identity não acessa `campanhas_db` e o contrário também vale. A comunicação entre os serviços é só por JWT e eventos de integração ([constituição, princípio II](../specs/constitution.md)).

## Custos assumidos

| Custo | Mitigação |
|---|---|
| Mais componentes para operar | Tudo sobe com um único `docker compose up` |
| Dados derivados podem ficar atrasados (cache, índice) | O atraso é aceito e documentado no painel (aviso de atualização) |
| Consistência eventual entre PostgreSQL, MongoDB e Elasticsearch | Consumidores idempotentes e reentrega at-least-once |
