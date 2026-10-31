# language: pt
Funcionalidade: Direitos do titular
  Como doador
  Quero ver e apagar meus dados
  Para exercer meus direitos da LGPD

  Cenário: Ver meus dados
    Dado um doador autenticado cadastrado com o CPF "529.982.247-25"
    Quando ele consulta os seus dados
    Então recebe status 200
    E vê o CPF como "***.982.247-**"
    E vê a versão e a data do aceite da política de privacidade

  Cenário: Anonimizar cadastro
    Dado um doador autenticado que já doou 100.00
    Quando ele solicita a exclusão dos seus dados
    Então recebe status 204
    E não consegue mais fazer login
    E o total da campanha continua incluindo os 100.00

  Cenário: Anonimizar cadastro sem doações
    Dado um doador autenticado cadastrado com o CPF "529.982.247-25"
    Quando ele solicita a exclusão dos seus dados
    Então recebe status 204
    E não consegue mais fazer login

  Cenário: Token após anonimização
    Dado um doador que acabou de anonimizar o cadastro
    Quando ele consulta os seus dados com o mesmo token
    Então recebe status 401

  Cenário: Reutilizar e-mail e CPF após anonimizar
    Dado um doador anonimizado que usava o e-mail "maria@exemplo.com"
    Quando um visitante se cadastra com o e-mail "maria@exemplo.com"
    Então recebe status 201
