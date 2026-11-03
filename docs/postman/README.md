# Coleção Postman

[← README](../../README.md)

Importe `conexao-solidaria.postman_collection.json` e `local.postman_environment.json` e selecione o ambiente **Local (docker-compose)**.

1. Preencha `gestorEmail` e `gestorSenha` no ambiente com `SEED_GESTOR_EMAIL` e `SEED_GESTOR_SENHA` de `deploy/.env` (o arquivo não vai para o Git).
2. Rode as pastas na ordem: **1** (login do gestor), **2** (campanhas), **3** (doador e doação), **4** (consultas públicas). Os tokens e o `idCampanha` são guardados em variáveis da coleção pelos scripts de teste.
3. Os itens marcados com *(erro)* confirmam as respostas de falha (401 e 400/422).

As URLs apontam direto para as APIs do docker-compose (`8081` e `8082`). O gateway Kong não faz parte desta entrega.
