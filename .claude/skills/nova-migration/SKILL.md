---
name: nova-migration
description: Cria e revisa uma migration do EF Core (IdentityDbContext ou CampanhasDbContext) com o script SQL gerado e checagem de perda de dados. Use quando uma tarefa alterar entidades, configurações EF ou tabelas.
---

# Nova migration do EF Core

Regras em `specs/product/dados.md` (seção de migrations).

## Contextos

| DbContext | Projeto da migration | Projeto de startup |
|---|---|---|
| `IdentityDbContext` | `src/Services/Identity/Identity.Infrastructure` | `src/Services/Identity/Identity.Api` |
| `CampanhasDbContext` | `src/Services/Campanhas/Campanhas.Infrastructure` | `src/Services/Campanhas/Campanhas.Api` |

## Passos

1. Confirme que a mudança no modelo compila: `dotnet build -c Release -warnaserror`.
2. Nome no padrão `AAAAMMDD_DescricaoEmPascalCase` só na descrição (o EF acrescenta o carimbo de tempo); ex.: `AdicionaChavesIdempotencia`.
3. Gere:
   ```bash
   dotnet ef migrations add <Nome> \
     --project <projeto da migration> \
     --startup-project <projeto de startup> \
     --context <DbContext> \
     --output-dir Persistencia/Migrations
   ```
4. Gere e leia o SQL:
   ```bash
   dotnet ef migrations script --idempotent \
     --project <projeto da migration> --startup-project <projeto de startup> --context <DbContext>
   ```
5. Revise:
   - [ ] nomes em `snake_case`;
   - [ ] `numeric(18,2)` para dinheiro; `timestamptz` para data-hora;
   - [ ] `CHECK`, `UNIQUE` e índices do `data-model.md` presentes;
   - [ ] nenhum `DROP COLUMN`/`DROP TABLE` ou mudança de tipo com perda de dados sem plano de migração de dados;
   - [ ] nenhuma migration já presente na `main` foi editada (correção vira migration nova).
6. Rode os testes de integração do serviço (Testcontainers aplica as migrations): `dotnet test tests/integration/<Servico>.IntegrationTests`.
7. Commit com a skill `commit-gitmoji` incluindo a migration, o designer e o snapshot do modelo: `🗃️ feat(<escopo>): <descrição>`.

## Regras

- As APIs não migram no startup; em ambiente, a migration roda pelo Job de *migration bundle* (`specs/product/infraestrutura.md`).
- Arquivos de `Migrations/` ficam fora da cobertura e da exigência de `<summary>` (código gerado).
