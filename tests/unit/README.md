# Testes de unidade

Base da pirâmide: rápidos, sem rede, sem banco e sem Docker. Rodam no hook `pre-commit` (`--filter Category=Unit`) e no CI.

| Projeto | Cobre |
|---|---|
| `Identity.UnitTests` | Domínio e aplicação da Identidade (`Email`, `Cpf`, `Senha`, `Usuario`, casos de uso) |
| `Campanhas.UnitTests` | Domínio e aplicação de Campanhas (`Dinheiro`, `Periodo`, `Campanha`, casos de uso) |
| `Arrecadacao.Worker.UnitTests` | Consumidor e configuração do Worker |
| `Notificacoes.Function.UnitTests` | Leitor do envelope, deduplicação e emissão do recibo |
| `ConexaoSolidaria.Web.UnitTests` | Componentes e páginas Blazor (bUnit) |

Cada projeto também tem o teste de arquitetura (NetArchTest) que impede dependências proibidas entre camadas.

## Regras

- xUnit v3 + Shouldly + NSubstitute; atributo `[Trait("Category", "Unit")]`.
- Nome: `Metodo_Cenario_ResultadoEsperado`; padrão Arrange–Act–Assert.
- Datas com `FakeTimeProvider`; nunca `DateTime.Now`.
- TDD obrigatório no domínio; `/// <summary>` em toda classe e método de teste descrevendo o cenário.

Estratégia completa: [docs/testes.md](../../docs/testes.md).
