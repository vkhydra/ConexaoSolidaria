# language: pt
Funcionalidade: Cadastro de doador
  Como visitante
  Quero me cadastrar como doador
  Para poder doar

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
    E o código do erro é "doador.email_em_uso"

  Cenário: CPF duplicado com outra máscara
    Dado um doador cadastrado com CPF "529.982.247-25"
    Quando outro visitante se cadastra com o CPF "52998224725"
    Então recebe status 409
    E o código do erro é "doador.cpf_em_uso"

  Esquema do Cenário: CPF inválido
    Quando um visitante se cadastra com CPF "<cpf>"
    Então recebe status 400

    Exemplos:
      | cpf            |
      | 529.982.247-26 |
      | 111.111.111-11 |
      | 123            |

  Esquema do Cenário: Senha fraca
    Quando um visitante se cadastra com a senha "<senha>"
    Então recebe status 400

    Exemplos:
      | senha        |
      | curta@1      |
      | semnumero@   |
      | Sem1Especial |

  Cenário: Sem aceite da política de privacidade
    Quando um visitante se cadastra sem aceitar a política de privacidade
    Então recebe status 400
