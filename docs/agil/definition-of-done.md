# Definition of Done (DoD)

[← README](../../README.md) · [Processo ágil](../processo-agil.md)

Um item só vai para **Concluído** quando todos os critérios abaixo estão atendidos. A pipeline do GitHub Actions confere o que for automático.

## Checklist

- [ ] Código segue os [padrões de código](../padroes-de-codigo.md), com `/// <summary>` em todos os tipos e membros.
- [ ] Testes de unidade escritos (TDD no domínio), com casos de **sucesso e de erro**, e cenários Gherkin da história automatizados.
- [ ] Cobertura de testes ≥ 90% de linhas e ramos, no total e em cada projeto de `src/`.
- [ ] Todos os commits no padrão e compilando ([git e commits](../git-e-commits.md)).
- [ ] CI verde: formato, build, testes unitários, testes de integração, cobertura e varreduras.
- [ ] `/code-review` executado e apontamentos tratados.
- [ ] Revisão de pelo menos um integrante no pull request.
- [ ] Logs, métricas e health checks do que foi criado.
- [ ] Nenhum dado pessoal em logs, eventos ou traces.
- [ ] Todas as tarefas do `tasks.md` marcadas `[X]` e `Status` da spec como `Implementada`.
- [ ] Documentação atualizada (README, spec, ADR quando houver decisão nova).
- [ ] Demonstrado rodando localmente.

## O que cada critério protege

| Critério | Risco que evita |
|---|---|
| Sucesso e erro nos testes | Funcionalidade que só funciona no caminho feliz |
| Cobertura ≥ 90% | Código sem teste chegando à `main` |
| `/// <summary>` | Código que ninguém entende depois |
| Commit que compila | Histórico impossível de usar com `git bisect` |
| Sem dados pessoais | Violação da LGPD |
