# language: pt
@busca
Funcionalidade: Busca de campanhas
  Como visitante
  Quero buscar campanhas mesmo com erro de digitação
  Para achá-las rápido

  Cenário: Erro de digitação
    Dado a campanha ativa "Natal Solidário"
    Quando um visitante busca "natl solidaro"
    Então "Natal Solidário" aparece no resultado

  Cenário: Título vale mais que descrição
    Dado a campanha ativa "Agasalho" com descrição "Roupas de inverno"
    E a campanha ativa "Inverno sem fome" com descrição "Cestas básicas"
    Quando um visitante busca "inverno"
    Então "Inverno sem fome" aparece antes de "Agasalho"

  Cenário: Campanha cancelada não aparece
    Dado a campanha cancelada "Inverno 2026"
    Quando um visitante busca "inverno"
    Então o resultado é vazio

  Cenário: Campanha nova entra na busca
    Dado um gestor que cria a campanha "Páscoa Feliz"
    Quando um visitante busca "pascoa" 5 segundos depois
    Então "Páscoa Feliz" aparece no resultado

  Cenário: Termo curto demais
    Quando um visitante busca "a"
    Então recebe status 400

  Cenário: Busca indisponível
    Dado o Elasticsearch fora do ar
    Quando um visitante busca "natal"
    Então recebe status 503
    E o painel de transparência continua respondendo
