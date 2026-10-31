# 04 — Requisitos funcionais

[← README](../../README.md) · Anterior: [03 — Processo ágil](../processo-agil.md) · Próximo: [05 — Requisitos não funcionais](../../specs/product/requisitos-nao-funcionais.md)

Cada requisito traz regras, validações, respostas e critérios de aceitação em Gherkin. Os cenários viram arquivos `.feature` em `tests/*.Bdd/Features/` ([15 — Testes](../testes.md)). Os contratos HTTP completos estão em [09 — API e eventos](../../specs/product/api-e-eventos.md).

Convenções:
- **400**: entrada mal formada (formato, campo obrigatório).
- **422**: entrada bem formada que viola regra de negócio.
- Todas as respostas de erro usam `ProblemDetails` com `code` estável (ex.: `campanha.meta_invalida`).
- "Hoje" é a data atual em `America/Sao_Paulo`, obtida por `TimeProvider`.

---

## RF01 — Autenticação e autorização (RBAC)

**Origem:** enunciado, requisito 1. **Serviço:** `Identity.Api`.

### Regras
1. Login com e-mail e senha. E-mail comparado em minúsculas.
2. Sucesso retorna JWT RS256 com validade de 60 minutos.
3. Claims: `sub` (ID do usuário), `role` (`GestorONG` ou `Doador`), `jti`, `iat`, `exp`, `iss`, `aud`. **Sem** nome, e-mail ou CPF no token.
4. Falha de login retorna 401 com mensagem genérica ("E-mail ou senha inválidos"), sem dizer qual campo errou.
5. Após 5 falhas seguidas no mesmo e-mail em 15 minutos, o login desse e-mail fica bloqueado por 15 minutos (429).
6. Usuário anonimizado não consegue entrar.
7. Policies: `GestorONG` (role `GestorONG`) e `Doador` (role `Doador`).
8. O primeiro `GestorONG` é criado no startup a partir de `SEED_GESTOR_EMAIL` e `SEED_GESTOR_SENHA` (Secret), só se não existir.

### Critérios de aceitação
```gherkin
Funcionalidade: Login
  Cenário: Login válido de doador
    Dado um doador cadastrado com e-mail "maria@exemplo.com" e senha "Senha@123"
    Quando ele faz login com essas credenciais
    Então recebe status 200
    E o token contém a role "Doador"
    E o token não contém e-mail nem CPF

  Cenário: Senha incorreta
    Dado um doador cadastrado com e-mail "maria@exemplo.com"
    Quando ele faz login com a senha "errada"
    Então recebe status 401
    E a mensagem é "E-mail ou senha inválidos"

  Cenário: Doador tenta acessar gestão de campanhas
    Dado um doador autenticado
    Quando ele lista as campanhas da gestão
    Então recebe status 403

  Cenário: Bloqueio por tentativas
    Dado 5 tentativas de login com senha incorreta para "maria@exemplo.com"
    Quando ele tenta entrar novamente com a senha correta
    Então recebe status 429
```

---

## RF02 — Gestão de campanhas

**Origem:** enunciado, requisito 2. **Serviço:** `Campanhas.Api`. **Acesso:** `GestorONG`.

### Campos

| Campo | Tipo | Validação de formato (400) | Regra de negócio (422) |
|---|---|---|---|
| `titulo` | texto | Obrigatório, 3 a 120 caracteres, sem espaços nas pontas | — |
| `descricao` | texto | Obrigatório, até 2000 caracteres | — |
| `dataInicio` | data (`yyyy-MM-dd`) | Obrigatória | — |
| `dataFim` | data (`yyyy-MM-dd`) | Obrigatória | Não pode ser anterior a hoje; não pode ser anterior a `dataInicio` |
| `metaFinanceira` | decimal | Obrigatória, até 2 casas decimais | Maior que zero; até 99.999.999,99 |
| `status` | enum | Definido pelo sistema | Nasce `Ativa` |

### Operações
1. **Criar:** status inicial `Ativa`, `totalArrecadado = 0`. Retorna 201 com `Location`.
2. **Listar:** paginado (`page` ≥ 1, `pageSize` 1–100, padrão 20), filtro opcional por `status`, ordenado por `dataFim`.
3. **Detalhar:** por ID; 404 se não existir.
4. **Editar:** só campanha `Ativa`. Mesmas validações da criação. Usa o cabeçalho `If-Match` com a versão (ETag) para concorrência otimista; versão divergente → 409.
5. **Mudar status:** `Ativa → Concluida` ou `Ativa → Cancelada`. Qualquer outra transição → 422. `totalArrecadado` nunca é editado manualmente.
6. Toda criação e alteração publica `CampanhaAlteradaEvent` (para o índice de busca).

### Critérios de aceitação
```gherkin
Funcionalidade: Gestão de campanhas
  Contexto:
    Dado um gestor autenticado
    E hoje é "2026-11-10"

  Cenário: Criar campanha válida
    Quando ele cria a campanha "Natal Solidário" de "2026-11-10" a "2026-12-24" com meta 10000.00
    Então recebe status 201
    E a campanha tem status "Ativa" e total arrecadado 0.00

  Esquema do Cenário: Criar campanha inválida
    Quando ele cria uma campanha com data fim "<dataFim>" e meta <meta>
    Então recebe status <status>
    E o código do erro é "<codigo>"

    Exemplos:
      | dataFim    | meta     | status | codigo                     |
      | 2026-11-09 | 1000.00  | 422    | campanha.data_fim_passada  |
      | 2026-12-01 | 0.00     | 422    | campanha.meta_invalida     |
      | 2026-12-01 | -10.00   | 422    | campanha.meta_invalida     |

  Cenário: Cancelar campanha concluída
    Dado uma campanha com status "Concluida"
    Quando ele tenta cancelar a campanha
    Então recebe status 422
    E o código do erro é "campanha.transicao_invalida"
```

---

## RF03 — Cadastro de doador

**Origem:** enunciado, requisito 3. **Serviço:** `Identity.Api`. **Acesso:** público.

### Campos

| Campo | Validação | Erro |
|---|---|---|
| `nomeCompleto` | 3 a 150 caracteres, ao menos duas palavras | 400 |
| `email` | Formato válido, até 254 caracteres, normalizado em minúsculas | 400 |
| `email` | Único no sistema | **409** `doador.email_em_uso` |
| `cpf` | Aceita com ou sem máscara; 11 dígitos; dígitos verificadores válidos; rejeita sequências repetidas (`111.111.111-11`) | 400 |
| `cpf` | Único no sistema (comparado pelo hash) | 409 `doador.cpf_em_uso` |
| `senha` | 8 a 64 caracteres, com letra, número e caractere especial | 400 |
| `aceitePoliticaPrivacidade` | Deve ser `true` | 400 |

### Regras
1. Senha armazenada só como hash **BCrypt** (work factor 12).
2. CPF armazenado cifrado (AES-256-GCM) e com hash HMAC-SHA256 para unicidade ([11 — Segurança e LGPD](../../specs/product/seguranca-e-lgpd.md)).
3. Registra a versão da política aceita e a data do aceite.
4. Role sempre `Doador`; não é possível se cadastrar como `GestorONG`.
5. Resposta 201 sem senha e com CPF mascarado (`***.456.789-**`).

### Critérios de aceitação
```gherkin
Funcionalidade: Cadastro de doador
  Cenário: Cadastro válido
    Quando um visitante se cadastra com nome "Maria Silva", e-mail "Maria@Exemplo.com", CPF "529.982.247-25" e senha "Senha@123"
    Então recebe status 201
    E o e-mail é salvo como "maria@exemplo.com"
    E a senha não é salva em texto puro
    E a resposta mostra o CPF como "***.982.247-**"

  Cenário: E-mail duplicado
    Dado um doador cadastrado com e-mail "maria@exemplo.com"
    Quando outro visitante se cadastra com o e-mail "MARIA@exemplo.com"
    Então recebe status 409

  Esquema do Cenário: CPF inválido
    Quando um visitante se cadastra com CPF "<cpf>"
    Então recebe status 400

    Exemplos:
      | cpf            |
      | 529.982.247-26 |
      | 111.111.111-11 |
      | 123            |
```

---

## RF04 — Painel de transparência

**Origem:** enunciado, requisito 4. **Serviço:** `Campanhas.Api`. **Acesso:** público.

### Regras
1. Lista só campanhas com status `Ativa` e `dataFim` ≥ hoje.
2. Campos: `id`, `titulo`, `metaFinanceira`, `totalArrecadado`, `percentualAtingido` (2 casas), `dataFim`.
3. `totalArrecadado` vem da soma das doações processadas pelo Worker (nunca da API de doação).
4. Resposta em cache (Redis via `HybridCache`) por 30 segundos; o Worker invalida o cache a cada doação processada.
5. Cabeçalho `Cache-Control: public, max-age=10`.
6. O total é **eventualmente consistente**: a UI mostra "Valores atualizados em até alguns segundos".

### Critérios de aceitação
```gherkin
Funcionalidade: Painel de transparência
  Cenário: Apenas campanhas ativas
    Dado as campanhas "A" (Ativa), "B" (Concluida) e "C" (Cancelada)
    Quando um visitante consulta o painel
    Então vê apenas a campanha "A"

  Cenário: Total após doação processada
    Dado a campanha "A" com total arrecadado 0.00
    Quando uma doação de 150.00 para "A" é processada pelo Worker
    Então em até 5 segundos o painel mostra total 150.00 para "A"
```

---

## RF05 — Doação

**Origem:** enunciado, requisito 5 e requisito técnico de mensageria. **Serviços:** `Campanhas.Api` (recebe) e `Arrecadacao.Worker` (processa). **Acesso:** `Doador`.

### Regras
1. Entrada: `idCampanha` (GUID) e `valorDoacao` (decimal, > 0, até 2 casas, máximo 1.000.000,00).
2. A campanha precisa existir (404) e estar **apta**: status `Ativa` e `dataInicio` ≤ hoje ≤ `dataFim`. Caso contrário → 422 `doacao.campanha_nao_ativa`.
3. A API **não** altera `totalArrecadado`. Ela gera `doacaoId` (GUID v7), grava `DoacaoRecebidaEvent` no Outbox na mesma transação e responde **202** com `doacaoId`.
4. Cabeçalho opcional `Idempotency-Key`: repetir a mesma chave em 24 h devolve o mesmo `doacaoId` sem publicar outro evento.
5. O Worker, ao consumir o evento:
   1. ignora se `doacaoId` já foi processado (idempotência);
   2. soma o valor ao total com `UPDATE` atômico, mesmo que a campanha tenha sido encerrada depois do aceite (a doação já foi aceita);
   3. grava o lançamento no livro de doações (MongoDB);
   4. invalida o cache do painel;
   5. publica `DoacaoProcessadaEvent`.
6. Falhas no Worker: 3 retentativas imediatas + 3 com backoff (5 s, 30 s, 2 min); depois a mensagem vai para a fila de erro (DLQ).

### Critérios de aceitação
```gherkin
Funcionalidade: Doação
  Contexto:
    Dado um doador autenticado

  Cenário: Doar para campanha ativa
    Dado a campanha "Natal Solidário" ativa
    Quando ele doa 150.00
    Então recebe status 202 com um doacaoId
    E o evento "DoacaoRecebidaEvent" é publicado
    E o total da campanha não é alterado pela API

  Esquema do Cenário: Doar para campanha não apta
    Dado a campanha "X" com status "<status>"
    Quando ele doa 50.00
    Então recebe status 422
    E nenhum evento é publicado

    Exemplos:
      | status    |
      | Concluida |
      | Cancelada |

  Cenário: Valor inválido
    Quando ele doa 0.00 para uma campanha ativa
    Então recebe status 400

  Cenário: Evento duplicado
    Dado um "DoacaoRecebidaEvent" de 100.00 já processado
    Quando o mesmo evento é entregue de novo ao Worker
    Então o total da campanha aumenta apenas 100.00
```

---

## RF06 — Busca de campanhas

**Origem:** Fase 4 (Elasticsearch). **Serviço:** `Campanhas.Api`. **Acesso:** público.

### Regras
1. `GET /campanhas/busca?q=<texto>&page=&pageSize=`; `q` com 2 a 100 caracteres.
2. Busca em `titulo` (peso 3) e `descricao` (peso 1) com `fuzziness: AUTO` e `prefix_length: 1`.
3. Correspondência exata no título recebe peso maior (aparece primeiro).
4. Só campanhas `Ativa` (filtro, não afeta a relevância).
5. Ordenação por relevância (`_score`); empate por `dataFim`.
6. Índice atualizado por evento em até 5 segundos após criar ou editar a campanha.
7. Se o Elasticsearch estiver fora do ar, responde 503 com `ProblemDetails` (sem derrubar o restante da API).

### Critérios de aceitação
```gherkin
Funcionalidade: Busca de campanhas
  Cenário: Erro de digitação
    Dado a campanha ativa "Natal Solidário"
    Quando um visitante busca "natl solidaro"
    Então "Natal Solidário" aparece no resultado

  Cenário: Campanha cancelada não aparece
    Dado a campanha cancelada "Inverno 2026"
    Quando um visitante busca "inverno"
    Então o resultado é vazio
```

---

## RF07 — Recibo de doação

**Origem:** Fase 3 (serverless). **Componente:** `Notificacoes.Function`.

### Regras
1. Disparado por `DoacaoProcessadaEvent`.
2. A função **não** consulta dados pessoais do doador. O recibo usa só os dados do evento (IDs, valor, total, data), respeitando a minimização, e se refere ao doador pelo ID.
3. "Envio" simulado: log estruturado `Information` com `ReciboEmitido`, `doacaoId`, `campanhaId`, `valor`.
4. Idempotente por `doacaoId` (reprocessar não gera recibo duplicado no log de negócio).
5. Escala a zero quando a fila está vazia (KEDA).

### Critério de aceitação
```gherkin
Funcionalidade: Recibo
  Cenário: Recibo após processamento
    Quando uma doação de 150.00 é processada
    Então a função registra "ReciboEmitido" com o doacaoId e o valor 150.00
    E o registro não contém nome, e-mail nem CPF
```

---

## RF08 — Direitos do titular (LGPD)

**Origem:** Fase 5 (LGPD). **Serviço:** `Identity.Api`. **Acesso:** `Doador`.

### Regras
1. `GET /doadores/me`: devolve nome, e-mail, CPF mascarado, data de cadastro, versão e data do aceite.
2. `DELETE /doadores/me`: **anonimiza** o cadastro:
   - nome → `"Doador anonimizado"`, e-mail → `anonimo+<id>@invalid`, CPF cifrado e hash → removidos, senha → removida;
   - marca `anonimizadoEm`; o login deixa de funcionar;
   - as doações continuam no total e no livro de doações (prestação de contas), ligadas só ao ID, que não identifica mais ninguém.
3. A operação é registrada em log de auditoria sem dados pessoais.
4. Responde 204; repetir a chamada com o mesmo token responde 401 (token de usuário anonimizado é rejeitado).

### Critério de aceitação
```gherkin
Funcionalidade: Direitos do titular
  Cenário: Anonimizar cadastro
    Dado um doador autenticado que já doou 100.00
    Quando ele solicita a exclusão dos seus dados
    Então recebe status 204
    E não consegue mais fazer login
    E o total da campanha continua incluindo os 100.00
```
