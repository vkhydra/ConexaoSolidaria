# language: pt
Funcionalidade: Doação
  O doador doa para uma campanha apta; a API aceita a doação e publica um evento, e o Worker soma o valor ao total
  de forma assíncrona, sem perder nem duplicar doações.

  Contexto:
    Dado um doador autenticado

  Cenário: Doar para campanha ativa
    Dado a campanha "Natal Solidário" ativa
    Quando ele doa 150.00
    Então recebe status 202 com um doacaoId
    E o evento "DoacaoRecebidaEvent" é publicado
    E o total da campanha não é alterado pela API

  Esquema do Cenário: Doar para campanha não apta
    Dado a campanha "Natal Solidário" com status "<status>"
    Quando ele doa 50.00
    Então recebe status 422
    E o código do erro é "doacao.campanha_nao_ativa"
    E nenhum evento é publicado

    Exemplos:
      | status    |
      | Concluida |
      | Cancelada |

  Cenário: Campanha inexistente
    Quando ele doa 50.00 para uma campanha que não existe
    Então recebe status 404

  Cenário: Valor inválido
    Dado a campanha "Natal Solidário" ativa
    Quando ele doa 0.00 para uma campanha ativa
    Então recebe status 400

  Cenário: Mesma chave de idempotência
    Dado a campanha "Natal Solidário" ativa
    E uma doação feita com a chave de idempotência "abc-123"
    Quando ele repete a doação com a chave "abc-123"
    Então recebe status 202 com o mesmo doacaoId
    E apenas um evento é publicado

  Cenário: Worker soma ao total
    Dado a campanha "Natal Solidário" com total 0.00
    Quando ele doa 150.00
    Então em até 5 segundos o total da campanha é 150.00
    E o livro de doações tem um lançamento de 150.00

  Cenário: Evento duplicado
    Dado a campanha "Natal Solidário" com total 0.00
    E um "DoacaoRecebidaEvent" de 100.00 já processado
    Quando o mesmo evento é entregue de novo ao Worker
    Então o total da campanha aumenta apenas 100.00

  Cenário: Gestor não doa
    Dado um gestor autenticado
    Quando ele tenta doar 50.00
    Então recebe status 403
