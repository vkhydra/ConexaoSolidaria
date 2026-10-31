# language: pt
Funcionalidade: Login e controle de acesso
  Como doador ou gestor
  Quero entrar com e-mail e senha
  Para acessar minhas funções

  Cenário: Login válido de doador
    Dado um doador cadastrado com e-mail "maria@exemplo.com" e senha "Senha@123"
    Quando ele faz login com essas credenciais
    Então recebe status 200
    E o token contém a role "Doador"
    E o token não contém nome, e-mail nem CPF

  Cenário: E-mail com letras maiúsculas
    Dado um doador cadastrado com e-mail "maria@exemplo.com"
    Quando ele faz login com o e-mail "Maria@Exemplo.com" e a senha correta
    Então recebe status 200

  Cenário: Senha incorreta
    Dado um doador cadastrado com e-mail "maria@exemplo.com"
    Quando ele faz login com a senha "errada"
    Então recebe status 401
    E a mensagem é "E-mail ou senha inválidos"

  Cenário: E-mail inexistente
    Quando alguém faz login com o e-mail "ninguem@exemplo.com"
    Então recebe status 401
    E a mensagem é "E-mail ou senha inválidos"

  Cenário: Bloqueio por tentativas
    Dado 5 tentativas de login com senha incorreta para "maria@exemplo.com" em 15 minutos
    Quando ele tenta entrar novamente com a senha correta
    Então recebe status 429

  Cenário: Rota protegida sem token
    Quando alguém lista as campanhas da gestão sem token
    Então recebe status 401

  Cenário: Doador tenta acessar gestão de campanhas
    Dado um doador autenticado
    Quando ele lista as campanhas da gestão
    Então recebe status 403

  Cenário: Gestor inicial criado na implantação
    Dado as variáveis de seed do gestor configuradas
    Quando a Identity.Api inicia sem nenhum gestor no banco
    Então o gestor é criado com a role "GestorONG"
    E uma segunda inicialização não cria outro gestor
