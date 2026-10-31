# language: pt
Funcionalidade: Gestão de campanhas
  Como gestor
  Quero criar, editar, concluir e cancelar campanhas
  Para arrecadar e manter as campanhas corretas

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
      | dataFim    | meta    | status | codigo                    |
      | 2026-11-09 | 1000.00 | 422    | campanha.data_fim_passada |
      | 2026-12-01 | 0.00    | 422    | campanha.meta_invalida    |
      | 2026-12-01 | -10.00  | 422    | campanha.meta_invalida    |

  Cenário: Data fim anterior à data início
    Quando ele cria uma campanha de "2026-12-10" a "2026-12-01" com meta 1000.00
    Então recebe status 422
    E o código do erro é "campanha.periodo_invalido"

  Cenário: Editar campanha ativa
    Dado a campanha "Natal Solidário" ativa na versão "1"
    Quando ele altera a meta para 12000.00 informando a versão "1"
    Então recebe status 204

  Cenário: Edição concorrente
    Dado a campanha "Natal Solidário" já alterada para a versão "2"
    Quando ele altera a campanha informando a versão "1"
    Então recebe status 409
    E o código do erro é "campanha.conflito_de_versao"

  Cenário: Cancelar campanha concluída
    Dado uma campanha com status "Concluida"
    Quando ele tenta cancelar a campanha
    Então recebe status 422
    E o código do erro é "campanha.transicao_invalida"

  Cenário: Doador não gerencia campanhas
    Dado um doador autenticado
    Quando ele tenta criar uma campanha
    Então recebe status 403
