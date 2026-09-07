# Print — Sprint 01 Foundation Production Ready

> **Atalho versionado** (espelho de `issues/SPRINT-01.md` que está em `.gitignore:22-24` e não é commitado).  
> Este arquivo existe para garantir que o planejamento da Sprint 01 fique rastreável no git.

**PO:** Sueli | **Dev:** @dev-otavio | **Auditor:** @aud_carlos | **Data:** 2026-09-04

- **Escopo fechado (23 SP):** US-SQL-000 (5), US-SQL-001 (5), US-EVB-001 (8), US-EVB-002 (5)
- **Deferidos Sprint 02:** US-SQL-002, US-CLEAN-001, US-DI-001, US-PACK-001
- **Ordem:** US-SQL-000 → US-SQL-001  |  US-EVB-001 → US-EVB-002  (trilhas A/B paralelas)
- **Specs:** `issues/*.md` + `MIGRATION.md` (405 linhas) + `CHANGELOG.md` `[0.2.0-alpha]`
- **Handover dev:** `issues/HANDOVER-dev-otavio.md`
- **Checklist auditor:** `issues/CHECKLIST-aud-carlos.md`
- **Sprint completa:** `issues/SPRINT-01.md` (148 linhas)

**Gate para dev começar:**
```bash
dotnet build -c Release # verde
grep -rn "SqlBuilder" --include="*.cs" src/ # ainda retorna SqlBuilder.cs antes do PASSO 1
```

**Gate para auditor aprovar:**
```bash
grep -rn "SqlBuilder" --include="*.cs" src/ # 0 após US-SQL-000
dotnet test && dotnet pack -c Release -o /tmp/pack
```

Ver `issues/SPRINT-01.md` para DoD completo, Diagrama Gantt Mermaid e Matriz de Dependências.
