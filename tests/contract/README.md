# Testes de contrato

Protegem os contratos públicos entre serviços. Se um contrato mudar sem intenção, o build quebra.

| Projeto | Cobre |
|---|---|
| `ConexaoSolidaria.ContractTests` | Serialização JSON dos eventos v1 (`DoacaoRecebidaEvent`, `DoacaoProcessadaEvent`, `CampanhaAlteradaEvent`) comparada com snapshots versionados; documento OpenAPI de cada API comparado com o snapshot |

## Regras

- Snapshot em `Snapshots/*.json`, revisado no PR quando mudar.
- Mudança incompatível cria um contrato `V2`; o `V1` continua com o snapshot original ([API e eventos](../../specs/product/api-e-eventos.md)).
- Atributo `[Trait("Category", "Contract")]`.
