# Glossário

[← README](../README.md)

Linguagem ubíqua do Conexão Solidária (`wiki:conceitos/linguagem-ubiqua`). Os termos de negócio são usados igualmente no código, na API, nos testes, nas specs e na interface.

## Termos de negócio

| Termo | Definição | No código |
|---|---|---|
| Anonimização | Remoção irreversível dos dados pessoais do doador, mantendo as doações para prestação de contas | `Usuario.Anonimizar()` |
| Campanha | Ação de arrecadação com meta e período | `Campanha` |
| Campanha ativa | Campanha com status `Ativa` | `StatusCampanha.Ativa` |
| Campanha apta | Campanha ativa cuja data de hoje está entre o início e o fim; só ela recebe doações | `Campanha.PodeReceberDoacao(hoje)` |
| Campanha cancelada | Encerrada pelo gestor antes do fim, sem conclusão | `StatusCampanha.Cancelada` |
| Campanha concluída | Encerrada pelo gestor após o período ou a meta | `StatusCampanha.Concluida` |
| Consentimento | Aceite da política de privacidade, com versão e data | `Consentimento` |
| Doação processada | Doação já somada ao total pelo Worker | `DoacaoProcessadaEvent` |
| Doação recebida | Doação aceita pela API e ainda não somada ao total | `DoacaoRecebidaEvent` |
| Doador | Pessoa cadastrada que faz doações | `Usuario` com `Role.Doador` |
| Gestor da ONG | Usuário que administra campanhas | `Usuario` com `Role.GestorONG` |
| Livro de doações | Registro imutável de todas as doações processadas | coleção `lancamentos_doacao` |
| Meta financeira | Valor que a campanha pretende arrecadar | `MetaFinanceira` |
| Painel de transparência | Visão pública das campanhas ativas e do total arrecadado | `GET /transparencia/campanhas` |
| Percentual atingido | Total arrecadado dividido pela meta, × 100, com 2 casas | `Campanha.PercentualAtingido` |
| Recibo | Comprovante simulado da doação processada | `Notificacoes.Function` |
| Titular | Pessoa a quem os dados pessoais se referem (LGPD) | — |
| Total arrecadado | Soma das doações **processadas** da campanha | `TotalArrecadado` |
| Visitante | Qualquer pessoa sem login | — |

## Termos técnicos

| Termo | Definição |
|---|---|
| Agregado | Conjunto de objetos de domínio tratado como uma unidade de consistência, acessado pela raiz (`Campanha`, `Usuario`) |
| At-least-once | Garantia de entrega em que a mensagem chega pelo menos uma vez e pode chegar repetida |
| Artefato (CI) | Arquivo publicado por uma execução da pipeline para download, como o relatório de cobertura |
| Bounded context | Fronteira em que um modelo de domínio é válido (Identidade, Campanhas, Notificações) |
| Cobertura de código | Percentual de linhas e de ramos do código executados pelos testes |
| Consistência eventual | O total do painel fica correto alguns segundos depois da doação, não no mesmo instante |
| Constituição | Princípios inegociáveis do projeto ([specs/constitution.md](../specs/constitution.md)) |
| DLQ (fila de erro) | Fila `*_error` que recebe mensagens que falharam após todas as retentativas |
| DoD / DoR | Definition of Done e Definition of Ready ([processo ágil](processo-agil.md)) |
| Feature | Funcionalidade especificada em `specs/features/NNN-nome/` |
| HPA | Horizontal Pod Autoscaler: escala pods por CPU |
| Idempotência | Repetir a mesma operação produz o mesmo resultado, sem efeito duplicado |
| KEDA | Autoscaler do Kubernetes por eventos; escala o Worker e a função pela fila, inclusive a zero |
| Outbox (Transactional Outbox) | Tabela onde o evento é gravado na mesma transação dos dados e entregue depois ao broker |
| RF / RNF | Requisito funcional / não funcional |
| SDD | Spec-Driven Development: especificar antes de implementar ([ADR 0017](adr/0017-spec-driven-development.md)) |
| SLO | Objetivo de nível de serviço ([requisitos não funcionais](../specs/product/requisitos-nao-funcionais.md)) |
| Trabalho substitutivo | Atividade Substitutiva da Fase 3: API .NET, testes unitários e CI com cobertura ([ADR 0018](adr/0018-escopo-trabalho-substitutivo.md)) |
| Value object | Objeto definido pelo valor, imutável e sem identidade (`Dinheiro`, `Periodo`, `Cpf`, `Email`) |
