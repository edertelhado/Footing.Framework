# Changelog — Footing.Framework

Todas as mudanças notáveis deste projeto são documentadas neste arquivo.

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e versionamento [SemVer](https://semver.org/lang/pt-BR/).

---

## [3.5.1] - 2026-09-07

### Fixed

- Internacionalização do runtime para inglês (mensagens de erro, warnings e XML docs)

### Changed

- `Version 3.5.1` patch sem breaking — benchmark formal via `BenchmarkDotNet` (PrivateAssets)

---

## [3.5.0] - 2026-09-07

### Added

- Portal de documentação Antora (`docs/modules/ROOT/pages/*.adoc`) — 13 páginas, build `docs/build/site`

### Notas

- Framework genérico e agnóstico a banco (PostgreSQL, Firebird, dbf via SPI). Nenhum usuário em produção antes de `3.5.x` — sem necessidade de guia de migração.

---

## [3.4.2] - 2026-09-06

### Changed

- Estabilização sem código — promoção para stable (11 regex, agnóstico verificado)

---

## [3.0.0] - 2026-09-06

### Removed — BREAKING

- `TypeHandlerRegistry` removido — use `SqlMapper.AddTypeHandler` (Dapper nativo)
- `AddDependencyWalkAuto` removido — use `AddDependencyWalk` com assemblies explícitos

---

## [1.0.0] - 2026-09-04

### Added

- EventBus in-process (`Channel` bounded), Data Layer (`SqlTemplate` + Dapper, `SqlBatch`), DI auto-scan, Lifecycle e InjectConfig

---

Histórico completo arquivado em `.opencode/archive/CHANGELOG-full.md` (não publicado).
