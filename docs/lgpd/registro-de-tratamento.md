# Registro das operações de tratamento de dados pessoais

[← Segurança e LGPD](../../specs/product/seguranca-e-lgpd.md)

**Versão:** 1.0 · **Data:** 2026-10-31 · **Controlador:** ONG Esperança Solidária · **Operador:** equipe que mantém a plataforma Conexão Solidária

> Documento técnico de um projeto acadêmico; não é orientação jurídica. O encarregado (DPO) é fictício.

## Operações

| # | Finalidade | Dados tratados | Base legal (LGPD, art. 7º) | Onde ficam | Retenção |
|---|---|---|---|---|---|
| 1 | Cadastro e autenticação do doador | Nome, e-mail, senha | Execução de contrato (V) | Identity (PostgreSQL); a senha só como hash BCrypt | Até a anonimização a pedido do titular |
| 2 | Identificação fiscal do doador | CPF | Consentimento (I), dado no aceite da política de privacidade | Identity (PostgreSQL); cifrado com AES-GCM, com hash HMAC só para checar unicidade | Até a anonimização a pedido do titular |
| 3 | Prestação de contas das doações | Identificador pseudônimo do doador, valor, data | Legítimo interesse (IX), com teste de balanceamento abaixo | Campanhas (PostgreSQL) e livro de doações (MongoDB) | Indefinida; sem dado pessoal depois da anonimização do cadastro |
| 4 | Segurança e diagnóstico | Identificadores, endereço IP no gateway | Legítimo interesse (IX) | Logs, métricas e traces | 7 dias |

## Teste de balanceamento do legítimo interesse (operação 3)

| Pergunta | Resposta |
|---|---|
| Qual o interesse legítimo? | Prestar contas às pessoas que doam e à sociedade sobre o dinheiro arrecadado. |
| O tratamento é necessário? | Sim: o total arrecadado só é confiável se cada doação for registrada uma única vez. |
| Qual o impacto ao titular? | Baixo: o registro usa só um identificador pseudônimo, valor e data; nome, e-mail e CPF não saem do cadastro. |
| Há salvaguardas? | Sim: depois da anonimização do cadastro, o identificador não aponta mais para nenhuma pessoa. |

## Compartilhamento

Nenhum dado pessoal é compartilhado com terceiros. Os eventos entre os serviços (doação recebida, doação processada, campanha alterada) carregam só identificadores e valores.

## Direitos do titular

| Direito | Como é atendido |
|---|---|
| Confirmação e acesso (art. 18, I e II) | `GET /doadores/me` devolve os dados do cadastro, com o CPF mascarado. |
| Eliminação (art. 18, VI) | `DELETE /doadores/me` anonimiza o cadastro; as doações permanecem como registros anônimos. |
| Informação sobre o tratamento (art. 18, VII) | [Política de privacidade](politica-de-privacidade.md), exibida no cadastro. |

## Medidas de segurança

Autenticação por JWT, autorização por papel, endpoints protegidos por padrão, CPF cifrado, senha só como hash, segredos fora do código, nenhum dado pessoal em logs, métricas, traces ou eventos. Detalhes em [segurança e LGPD](../../specs/product/seguranca-e-lgpd.md).
