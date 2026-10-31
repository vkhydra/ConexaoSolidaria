---
name: documentar-xml
description: Acrescenta ou revisa comentários XML (/// <summary>, <param>, <returns>, <exception>, <inheritdoc/>) em todo tipo e membro C# alterado, no padrão do projeto. Use após escrever código C#, antes do commit, ou quando o build falhar por CS1591/SA16xx.
---

# Documentar código C# com `<summary>`

Regra da constituição (princípio V) e de `docs/padroes-de-codigo.md`: **todo tipo e membro, de qualquer visibilidade, inclusive testes, tem `/// <summary>` em português.**

## Passos

1. Liste os arquivos C# alterados:
   ```bash
   git diff --name-only --diff-filter=AM HEAD -- "*.cs"
   git diff --name-only --cached --diff-filter=AM -- "*.cs"
   ```
   Ignore `Migrations/`, `*.g.cs` e arquivos gerados.
2. Em cada arquivo, documente o que falta:

   | Elemento | Tags |
   |---|---|
   | Classe, record, struct, interface, enum, delegate | `<summary>`; `<typeparam>` se genérico; `<remarks>` para regra de negócio ou ADR |
   | Construtor e método | `<summary>`, `<param>` para cada parâmetro, `<returns>` se retorna valor (inclusive `Task<T>`), `<exception cref>` para exceções lançadas de propósito |
   | Propriedade, campo, constante, item de enum | `<summary>` |
   | Record posicional | `<param>` para cada propriedade no próprio record |
   | Implementação de interface e `override` | `/// <inheritdoc/>` (a documentação fica na interface ou na base) |
   | Classe e método de teste | `<summary>` com o cenário em Dado/Quando/Então |
   | Componente Blazor | No code-behind `.razor.cs` e em cada `[Parameter]` |

3. Escreva bem:
   - diga o propósito e a regra; não repita o nome ("Obtém o id." não serve);
   - cite o código de erro e o RF quando houver (`campanha.meta_invalida`, RF02);
   - frases completas, terminadas em ponto;
   - use `<see cref="..."/>` para tipos e membros e `<c>...</c>` para literais;
   - nunca coloque dados pessoais reais, segredos ou URLs internas.
4. Não altere comportamento: só comentários.
5. Valide:
   ```bash
   dotnet build -c Release -warnaserror
   ```
   CS1591 (falta documentação), CS1573/CS1572 (`<param>` errado), CS1574 (`cref` inválido) e SA16xx devem zerar.
6. Se a documentação for um commit à parte, use a skill `commit-gitmoji`: `💡 docs(<escopo>): documenta <o quê>`. Durante o `/implement`, ela vai no mesmo commit da tarefa.

## Exemplo

```csharp
/// <summary>
/// Indica se a campanha pode receber doações na data informada (RF05).
/// </summary>
/// <param name="hoje">Data atual em America/Sao_Paulo, obtida do <see cref="TimeProvider"/>.</param>
/// <returns><c>true</c> se a campanha está ativa e a data está dentro do período.</returns>
public bool PodeReceberDoacao(DateOnly hoje)
```
