# language: pt
Funcionalidade: Painel de transparência
  Como visitante
  Quero ver campanhas ativas e o total arrecadado
  Para confiar na ONG

  Cenário: Apenas campanhas ativas
    Dado as campanhas "A" (Ativa), "B" (Concluida) e "C" (Cancelada)
    Quando um visitante consulta o painel
    Então vê apenas a campanha "A"

  Cenário: Campanha com prazo encerrado não aparece
    Dado a campanha "Natal Solidário" ativa com data fim "2026-12-24"
    E hoje é "2026-12-25"
    Quando um visitante consulta o painel
    Então a campanha "Natal Solidário" não aparece

  Cenário: Campanha que ainda não começou não aparece
    Dado hoje é "2026-11-10"
    E a campanha "Inverno" ativa com data início "2026-12-01"
    Quando um visitante consulta o painel
    Então a campanha "Inverno" não aparece

  Cenário: Percentual atingido
    Dado a campanha "A" com meta 10000.00 e total arrecadado 150.00
    Quando um visitante consulta o painel
    Então o percentual atingido de "A" é 1.50

  # Depende da doação processada pelo Worker (feature 005); liberado em R124.
  @ignore
  Cenário: Total após doação processada
    Dado a campanha "A" com total arrecadado 0.00
    Quando uma doação de 150.00 para "A" é processada pelo Worker
    Então em até 5 segundos o painel mostra total 150.00 para "A"

  Cenário: Painel funciona sem o cache
    Dado o Redis fora do ar
    E a campanha "A" com meta 10000.00 e total arrecadado 150.00
    Quando um visitante consulta o painel
    Então recebe status 200 com os dados do banco
