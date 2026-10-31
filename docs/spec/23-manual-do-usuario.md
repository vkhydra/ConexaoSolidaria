# 23 — Manual do usuário

[← README](../../README.md) · Anterior: [22 — Manual de implementação](22-manual-de-implementacao.md)

Como usar a plataforma **Conexão Solidária** pelo navegador. Público: visitantes, doadores e gestores da ONG Esperança Solidária. Para instalar e colocar no ar, ver [22 — Manual de implementação](22-manual-de-implementacao.md).

## 23.1 Perfis de acesso

| Perfil | Quem é | O que pode fazer |
|---|---|---|
| Visitante | Qualquer pessoa, sem login | Ver o painel de transparência, buscar campanhas, criar conta de doador |
| Doador | Pessoa cadastrada | Tudo do visitante + doar, ver e apagar os próprios dados |
| Gestor da ONG (`GestorONG`) | Equipe da ONG | Tudo do visitante + criar, editar, concluir e cancelar campanhas |

Não existe cadastro público de gestor: o primeiro gestor é criado na implantação.

## 23.2 Acesso

| Ambiente | Endereço |
|---|---|
| Kubernetes local (padrão) | https://app.conexao.local |
| docker-compose | http://localhost:8080 |

- Navegadores: versões atuais de Chrome, Edge, Firefox ou Safari, em computador ou celular.
- Usuários de demonstração: o gestor e os doadores criados pelo script de demonstração estão listados no README do repositório.
- A sessão dura **60 minutos**. Depois disso, a plataforma pede login de novo.

## 23.3 Mapa das telas

| Tela | Endereço | Perfil |
|---|---|---|
| Início | `/` | Todos |
| Painel de transparência | `/transparencia` | Todos |
| Busca de campanhas | `/busca` | Todos |
| Criar conta | `/cadastro` | Visitante |
| Entrar | `/entrar` | Visitante |
| Doar | `/campanhas/{id}/doar` | Doador |
| Meus dados | `/meus-dados` | Doador |
| Gestão de campanhas | `/gestao/campanhas` | Gestor |
| Nova campanha | `/gestao/campanhas/nova` | Gestor |
| Editar campanha | `/gestao/campanhas/{id}` | Gestor |

O menu do topo mostra só as opções do perfil de quem está logado.

## 23.4 Visitante

### Ver o painel de transparência
1. Clicar em **Transparência** no menu.
2. Cada cartão mostra uma campanha **ativa**:

   | Campo | Significado |
   |---|---|
   | Título | Nome da campanha |
   | Meta | Valor que a ONG quer arrecadar |
   | Arrecadado | Soma das doações já processadas |
   | Barra de progresso | Percentual da meta atingido |
   | Termina em | Último dia para doar |

3. Campanhas concluídas, canceladas ou com prazo encerrado não aparecem.
4. O valor arrecadado é atualizado **em até alguns segundos** depois de cada doação.

### Buscar campanhas
1. Digitar pelo menos 2 letras no campo **Buscar** e pressionar Enter.
2. A busca aceita erros de digitação: "natl solidaro" encontra "Natal Solidário".
3. Os resultados vêm em ordem de relevância, com o trecho que combinou destacado.
4. Só aparecem campanhas ativas.

### Criar conta de doador
1. Clicar em **Criar conta**.
2. Preencher:

   | Campo | Regra |
   |---|---|
   | Nome completo | Nome e sobrenome, de 3 a 150 caracteres |
   | E-mail | Válido e ainda não usado na plataforma |
   | CPF | Válido, com ou sem pontos e traço; um cadastro por CPF |
   | Senha | De 8 a 64 caracteres, com letra, número e caractere especial (ex.: `@`, `#`, `!`) |
   | Política de privacidade | Ler e marcar o aceite (obrigatório) |

3. Clicar em **Cadastrar**. A conta é criada como **Doador** e a tela de login abre em seguida.
4. Depois do cadastro, o CPF aparece sempre mascarado (ex.: `***.982.247-**`).

## 23.5 Doador

### Entrar e sair
1. Clicar em **Entrar**, informar e-mail e senha.
2. Se errar, a mensagem é sempre "E-mail ou senha inválidos", sem dizer qual dos dois está errado.
3. Depois de **5 tentativas erradas em 15 minutos**, o login desse e-mail fica bloqueado por 15 minutos.
4. Para sair, clicar no nome no topo → **Sair**.

### Doar
1. No painel ou na busca, escolher a campanha e clicar em **Doar**.
2. Informar o valor (maior que zero, até R$ 1.000.000,00, com até 2 casas decimais).
3. Clicar em **Confirmar doação**.
4. A tela mostra "Doação recebida. O total será atualizado em instantes." e o número da doação.
5. Em poucos segundos, o valor aparece no total da campanha no painel.
6. Clicar duas vezes em **Confirmar** não gera doação duplicada.

Só é possível doar para campanha **ativa** e dentro do período (entre a data de início e a data de término).

### Recibo
Cada doação processada gera um recibo com o número da doação, a campanha, o valor e a data. Nesta versão (MVP), o envio do recibo é **simulado**: ele fica registrado pela plataforma, mas não chega por e-mail.

### Meus dados (LGPD)
1. Clicar no nome no topo → **Meus dados**.
2. A tela mostra nome, e-mail, CPF mascarado, data de cadastro e a versão da política de privacidade aceita.
3. Para apagar os dados pessoais, clicar em **Excluir meus dados** e confirmar.

O que acontece ao excluir (não pode ser desfeito):

| Item | Resultado |
|---|---|
| Nome, e-mail, CPF e senha | Apagados (substituídos por valores anônimos) |
| Login | Deixa de funcionar; a sessão atual é encerrada |
| Doações já feitas | Continuam somadas nas campanhas, para prestação de contas, sem nenhum dado que identifique a pessoa |
| Nova conta | Pode ser criada depois com o mesmo e-mail e CPF |

## 23.6 Gestor da ONG

### Ver as campanhas
1. Entrar com o usuário de gestor e clicar em **Gestão**.
2. A lista mostra todas as campanhas, com filtro por status (**Ativa**, **Concluída**, **Cancelada**) e ordenadas pela data de término.

### Criar campanha
1. Clicar em **Nova campanha**.
2. Preencher:

   | Campo | Regra |
   |---|---|
   | Título | De 3 a 120 caracteres |
   | Descrição | Até 2000 caracteres |
   | Data de início | Obrigatória |
   | Data de término | Não pode estar no passado nem ser anterior à data de início |
   | Meta financeira | Maior que zero, até R$ 99.999.999,99 |

3. Clicar em **Salvar**. A campanha nasce **Ativa**, com arrecadado R$ 0,00.
4. Em poucos segundos ela aparece na busca e, se já estiver no período, no painel.

**Assistente de descrição (se habilitado):** o botão **Sugerir descrição** gera um texto a partir do título e de tópicos digitados. O texto é marcado como gerado por IA e **deve ser revisado** antes de salvar. Nenhum dado de doador é usado.

### Editar campanha
1. Na lista, clicar na campanha → **Editar**.
2. Só campanhas **ativas** podem ser editadas, com as mesmas regras da criação.
3. O valor arrecadado não pode ser alterado à mão: ele vem só das doações.
4. Se outra pessoa salvou a mesma campanha antes, aparece o aviso "A campanha foi alterada por outra pessoa". Recarregar a página e refazer a alteração.

### Concluir ou cancelar
1. Na campanha ativa, clicar em **Concluir** (meta ou prazo atingidos) ou **Cancelar** (campanha desistida).
2. Confirmar. A mudança **não pode ser desfeita** e a campanha sai do painel e da busca.
3. Doações aceitas antes da mudança ainda são somadas ao total.

## 23.7 Mensagens comuns

| Mensagem | Motivo | O que fazer |
|---|---|---|
| E-mail ou senha inválidos | Credenciais erradas | Conferir e tentar de novo |
| Muitas tentativas. Tente novamente em alguns minutos | Bloqueio por tentativas ou excesso de acessos | Esperar 15 minutos |
| Este e-mail já está em uso | E-mail já cadastrado | Entrar com ele ou usar outro |
| Este CPF já está cadastrado | CPF já tem conta | Entrar com a conta existente |
| CPF inválido | Dígitos verificadores não conferem | Conferir o número |
| A senha não atende aos requisitos | Política de senha | Usar 8+ caracteres com letra, número e caractere especial |
| A campanha não está aceitando doações | Campanha encerrada ou fora do período | Escolher outra campanha no painel |
| A data de término não pode estar no passado | Regra da campanha | Corrigir a data |
| A meta deve ser maior que zero | Regra da campanha | Corrigir o valor |
| A campanha não pode ser editada | Campanha concluída ou cancelada | Nenhuma ação: só campanhas ativas são editáveis |
| A campanha foi alterada por outra pessoa | Edição simultânea | Recarregar e refazer |
| Busca temporariamente indisponível | Serviço de busca fora do ar | Usar o painel ou tentar mais tarde |
| Sua sessão expirou | Passaram 60 minutos | Entrar de novo |
| Você não tem permissão para esta página | Perfil sem acesso | Entrar com o perfil correto |

Toda mensagem de erro inesperado traz um **código de rastreio**. Informar esse código à equipe ajuda a encontrar o problema.

## 23.8 Privacidade

| Pergunta | Resposta |
|---|---|
| Quais dados são coletados? | Nome, e-mail, CPF (cifrado), senha (só o hash) e o aceite da política |
| Para quê? | Identificar o doador, evitar cadastros duplicados e prestar contas das doações |
| Quem vê o CPF? | Ninguém vê o número completo na plataforma; ele aparece sempre mascarado |
| Posso ver e apagar meus dados? | Sim, em **Meus dados** (23.5) |
| Com quem falar? | Encarregado de dados indicado na política de privacidade |

## 23.9 Perguntas frequentes

| Pergunta | Resposta |
|---|---|
| Doei, mas o total não mudou | O total é atualizado em até alguns segundos. Recarregar a página do painel |
| Posso cancelar uma doação? | Não pela plataforma nesta versão; falar com a ONG |
| Posso me cadastrar como gestor? | Não. Gestores são criados pela equipe da ONG |
| Esqueci a senha | A recuperação de senha não faz parte desta versão; falar com a ONG |
| Por que a campanha sumiu do painel? | Foi concluída, cancelada ou passou da data de término |
| A plataforma funciona no celular? | Sim, pelo navegador |

## 23.10 Uso pela API

Integrações e testes podem usar a API REST direto em `https://api.conexao.local/api/v1`, com a documentação em `/swagger` (ambiente local) e a coleção Postman em `docs/postman/`. Endpoints, exemplos e códigos de erro: [09 — API e eventos](09-api-e-eventos.md).
