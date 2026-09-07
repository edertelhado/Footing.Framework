# Changelog — Footing.Framework

Todas as mudanças notáveis deste projeto são documentadas neste arquivo.

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e versionamento [SemVer](https://semver.org/lang/pt-BR/).

---

## [3.5.1] - 2026-09-07

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.15 — `3.5.0 → 3.5.1` patch 0.35 SP sem or (Fixed i18n varredura PT→EN 0.15 + BenchmarkDotNet formal PrivateAssets 0.20 + and revalidação 0 SP, or Won't definitivo 0% 0/208 t-odonto vs and 34.1% 71/208 Must DONE).

### Fixed

- **i18n — varredura PT→EN 0.15 SP (US-I18N-varredura-codigo V01-V39 + T01-T09, print-44)** — `src/Footing.Framework/**/*.cs` **27 violations Must** + `tests/**/*.cs` **8 Should** (`c6e7e28 0.10 SRC + aa37af8 0.05 TESTS`) — `MigrationRunner` 4 throws PT→EN (`Pending failed migrations`, `Migration failed at`, `Checksum drift detected`), `SqlTemplate` 6 `Debug.WriteLine` warnings PT→EN (`or/()/!/and 3+ not supported`, `incompatible types`), 14 XML docs PT→EN (`agnostic`, `structural failure`, `explicit transaction` etc.), 3 `print-08` refs removidos, `tests/TestProviderRegistry` 2 throws PT→EN + `MigrationResilienceTests` 1 throw + método `Runner_Drop_Idempotent` EN. Runtime 100% EN (`grep -P "[áàâãéêíóôõúç]" src --include="*.cs" ==0`), `grep -rn "\.md" src --include="*.cs" | grep -v obj ==0` sem refs `.md` em runtime, `DDL transactions are not supported` 3× EN 108 chars sem `GLOBAL_RULES`/`migrations.md` refs mantido, docs externas PT OK (`docs/modules/ROOT/pages/*.adoc` Antora PT + snippet EN). `0 LOC prod` lógica, só strings/comments, `2897 LOC <3.5K`. **[Must 0.05 SP docs/tag — gate auditor i18n]**

### Changed

- **Packaging — `Version 3.5.0 → 3.5.1` patch** — `Footing.Framework.csproj:Version` `3.5.0 → 3.5.1` patch SemVer `Fixed i18n` + `BenchmarkDotNet formal PrivateAssets` (docs/perf, 0 breaking, 0 LOC prod obrigatório). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.5.1.nupkg + .snupkg ~78K` com `README+LICENSE+icon.png` (`unzip -l *.nupkg | grep icon.png`), `TWE true`, `Deterministic true`, `2897 LOC <3.5K`, `11→11 regex`, `477 Passed`, `0 Npgsql/src` (`grep -r Npgsql src --include="*.cs" | grep -v "DDL transactions are not supported" ==0`). **[Must 0.10 SP tag/publish]**

### Notas

- `v3.5.0` 477t 11 regex 78K 2897 LOC → `v3.5.1` 477t (ou 477→477 benchmark terceirizado 0 LOC prod) 11 regex 78K 2897 LOC (0 LOC prod docs only + `tests/Footing.Framework.Benchmarks` PrivateAssets `BenchmarkDotNet`), `or` flat 0.5 Won't definitivo 0% 0/208 t-odonto 44 xml vs `and` 34.1% 71/208 Must DONE v2.3.0-alpha 11 testes, `and 3+` 0.25 Won't definitivo, `full OGNL !/()/user.name` 3-5 Won't definitivo sem gatilho BUY, SG 2.5 Won't, Sample 0 Won't. Tag `v3.5.1` só após auditor @aud_carlos APROVADO ≥9.0 (0 Npgsql/src, TWE true, `GLOBAL_RULES 60L`, docs/build/site 13 html 0 erro).
- Links: `[3.5.1]: https://github.com/etelhado/Footing.Framework/compare/v3.5.0...v3.5.1` — manter `[3.5.0]` histórico.

---

## [3.5.0] - 2026-09-07

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.14 — `3.4.2 → 3.5.0` minor revisado 0.7 SP (S2 docs Must 0.3 + S1 UseTransaction docs 0.2 + S4 perf Could 0.2, Sample Could 3.5.1 gatilho BUY>30%, SG Won't).

### Added

- **Docs — Portal Antora Must 0.3 SP (US-DOCS-ANTORA-ADOC-3.5.0)** — `docs/antora.yml version 3.5 display 3.5.0` + `antora-playbook.yml site.title + ui.bundle + output docs/build/site` + `docs/modules/ROOT/nav.adoc 13 entries` + `pages/*.adoc 10 Must (index landing mermaid + quickstart 3 comandos + sql-template 11 tags Find.sql 11 tags in 1 query + and/length/IFDEFINED/BIND/INCLUDE + tri-state Y/N + fail-fast S1-S6, sql-batch VALUES chunked 500/2100 + columnsOverride + Stopwatch Smoke perf, di Walk 50ms vs SG Won't print-26 + DiSmoke, eventbus Channel bounded FullMode.Wait + Priority + IEventHandler, migrations SPI Y/N CHAR(1) + VersionParser + Checksum + UseTransaction opt-in docs-only 20L, outbox-idempotency SPI tabs PG/Firebird/dbf include::example$, otel-health ActivitySource + Health /health, configuration GLOBAL_RULES 59L gates) + `recipes/bulk-insert,paging,chaos` + `partials/_attributes.adoc :footing-version 3.5.0` + `examples/Outbox 3 stores` + `README badge docs/antora`. `npx antora antora-playbook.yml` → `docs/build/site/footing-framework/3.5/*.html 13 html 0 erro` + `build/site` mirror. `0 LOC prod` docs only, `2878 LOC <3.5K`, `11→11 regex`, `473 Passed`, `0 Npgsql/src`, `TWE true`. **[Must 0.3 SP docs — stakeholder pediu, ROI adoção]**

- **Migrations — `UseTransaction opt-in` S1 Should 0.2 SP (US-MIG-USETRANSACTION-3.5.0 docs-only, sem dor PG >30% não há código prod)** — snippet `MigrationOptions.UseTransaction bool` `IDbTransaction` BCL `20L` em `migrations.adoc + otel-health.adoc` reuse `tx.Connection` para `Execute+Journal` atômico PG DDL. Só puxa código prod se dor PG >30% multi-statement. **Mantido docs-only em 3.5.0 revisado; código real Won't até gatilho.**

- **Perf — `BenchmarkSmoke` S4 Could 0.2 SP (US-GEN-PERF-POLISH-3.5.0)** — `sql-batch.adoc` + `otel-health.adoc` documenta `Stopwatch <2000ms 10k` já validado + `BenchmarkDotNet PrivateAssets` suite opcional. Sem código novo, só docs perf.

### Changed

- **Packaging — `Version 3.4.2 → 3.5.0` minor revisado 0.7 SP** — `Footing.Framework.csproj:Version` `3.4.2 → 3.5.0` minor SemVer additive docs Must (sem breaking, sem `IRepository<T>`). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.5.0.nupkg + .snupkg ~78K` com `README+LICENSE+icon.png`, `TWE true`, `2878 LOC <3.5K`, `11→11 regex`, `473 Passed`, `Sample Could 3.5.1 adiado`.

### Notas

- `v3.4.2` 473t 11 regex 78K 2878 LOC → `v3.5.0` 473t 11 regex 78K 2878 LOC (0 LOC prod docs only, +1485 LOC docs .adoc), `Sample Academia 13 features` rebaixado `Should 0.3 → Could 3.5.1` com gatilho `BUY >30% OU ≥3 pedidos OU demo venda` (ver `print-41.md` + `issues/US-SAMPLE-vs-DOCS-adoc.md` + `MIGRATION.md §8.14`). Tag `v3.5.0` só após auditor @aud_carlos APROVADO ≥9.0 (0 Npgsql/src, TWE true, docs/build/site 10 pages).
- Links: `[3.5.0]: https://github.com/etelhado/Footing.Framework/compare/v3.4.2...v3.5.0` — manter `[3.4.2]` histórico.

---

## [3.4.2] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.13 — `3.3.0 → 3.4.2` stable promoção sem código (0 LOC prod, +80 testes GilsonChaos, 473t). Sem breaking fora `and` simples já stable em `2.3.0`.

### Changed

- **Packaging — `Version 3.4.1 → 3.4.2` patch stable sem sufixo (US-DOC-3.4.2 0.2 SP promoção)** — `Footing.Framework.csproj:Version` `3.4.1 → 3.4.2` patch SemVer stable sem sufixo (sem `-alpha`). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.4.2.nupkg + .snupkg ~78K` com `README+LICENSE+icon.png` (`unzip -l *.nupkg | grep icon.png`), `TWE true` 0 erro, `3188 LOC <3.5K`, `11→11 GeneratedRegex` sem novo regex, `473 Passed 0 Skipped`. **[Must 0.2 SP docs/tag — estabilização sem código sobre d61ef60]**

### Notas

- `v3.3.0` 371 testes, 11 regex, pack 68K, 2877 LOC → `v3.4.2` 473 testes (+102: `3.4.0` resiliência 18 falha>feliz sqlite:memory + `3.4.1` 4 DBs fixtures 0.5 SP + `3.4.2` GilsonChaos 80 C1-C9+and/BIND/INCLUDE/TRIM/SET/IFDEFINED/unicode), `11→11 regex` sem novo `GeneratedRegex`, `2877→3188 LOC +311 <3.5K` TWE true strict, `pack 68K→78K`, `GLOBAL_RULES 59L` agnóstico, `0 Npgsql/Firebird/OleDb/Redis` em `src/` (`grep Npgsql src ==0`), `Version 3.4.2` stable. Histórico `v3.4.0` 389t (CRLF normalize + sqlite:memory `SqliteInMemoryFactory` + 18 testes M1-M6) + `v3.4.1` 393t (TestProviderRegistry 4 DBs + 20 .sql fixtures) já packados em `3.4.2` sem bump adicional prod — ver `MIGRATION.md §8.13` + `print-38.md` roadmap `3.4.2→3.5.0→4.0.0` + `PUBLISH.md 3.4.2`. Tag `v3.4.2` só após auditor @aud_carlos APROVADO ≥9.0 (não taguear nesta sprint — deixar para auditoria).
- Links: `[3.4.2]: https://github.com/etelhado/Footing.Framework/compare/v3.3.0...v3.4.2` — manter `[3.3.0]`, `[3.2.0]` histórico.

---

## [3.3.0] - 2026-09-07

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.12 — `3.2.0 → 3.3.0` minor additive agnóstico Y/N.

### Added

- **Migrations — `Migrations SPI carteiro Y/N` 0.7 SP (US-MIGRATIONS-SPI-001..006)** — `src/Footing.Framework/Migrations/` `IMigrationJournal + IMigrationScriptProvider + IMigrationRunner` SPI tripartite async `IAsyncEnumerable<MigrationInfo>` + `IDbConnectionFactory` agnóstico, `MigrationVersionParser` `V1__/V1_0_1__/R__/RNNN__/NNN__` sem regex (`IndexOf "__"`), `MigrationChecksum` SHA256, `MigrationRunner` carteiro 60 LOC `EnsureHistoryTable → GetFailed check → OrderBy Version → ExecuteRawSql IDbCommand + InsertHistory Y/N → catch Insert N + throw MigrationException → Repeatables checksum diff Upsert → Repair DELETE WHERE success='N' → Validate drift → Baseline → GenerateScript dry-run + placeholders `Dictionary Replace` (`{{key}}`), `FileSystemMigrationScriptProvider` + `EmbeddedResourceMigrationScriptProvider`, `MigrationException` + `MigrationInfo/Result/Status`, 11→11 regex, `BoolCharYNTypeHandler` `bool→'Y'/'N'` `DbType.AnsiStringFixedLength Size=1`, `docs/examples/Migrations/DDL/__migrations.sql` `CHAR(1) Y/N CHECK (success IN ('Y','N')) + VARCHAR(4000)` 100% agnóstico (PG/MSSQL/FB/dbf), `tests/MigrationTests.cs` 7 testes parser/checksum/drift/Y/N/runner.

### Changed

- **Packaging — `Version 3.2.0 → 3.3.0` minor** — `Footing.Framework.csproj:Version` `3.2.0 → 3.3.0` minor SemVer additive agnóstico Y/N. `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.3.0.nupkg + .snupkg ~68K` com `README+LICENSE+icon.png` (`unzip -l`), `TWE true` 0 erro, `2877 LOC <3.5K`, `11→11 regex`, `371 Passed`.

### Notas

- `v3.2.0` 364 testes, 11 regex, pack 66K → `v3.3.0` 371 testes (+7: VersionParser 4 + Checksum 1 + YN 1 + Runner 1), 11→11 regex, pack 68K, 2877 LOC <3.5K tese, `Migrations/` SPI Y/N 100% agnóstico `CHAR(1) Y/N` (ver `MIGRATION.md §8.12` + `print-32.md` Y/N + `docs/examples/Migrations/DDL/__migrations.sql`). Tag `v3.3.0` só após auditor APROVADO ≥9.0.
- Links: `[3.3.0]: https://github.com/etelhado/Footing.Framework/compare/v3.2.0...v3.3.0` — manter `[3.2.0]` histórico.

---

## [3.2.0] - 2026-09-07

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.11 — `3.1.0 → 3.2.0` minor additive sem breaking (1.5 SP sem migrations — DiSmoke 0.25 + Spec 0.5 + Benchmark 0.5 + Docs 0.25, 2449→2480 LOC, 356→364t, 11→11 regex, 0 deps DB, Version 3.2.0).

### Added

- **DI — `DiSmokeTests` 0.25 SP (US-GEN-DI-SMOKE-001)** — `tests/Footing.Framework.Tests/DiSmokeTests.cs` 2 testes agnósticos `GetServices<IRepo>.Count==1 + ValidateScopes` sem DB, detecta assembly esquecido sem SG, sem `Migrations/` (fica 3.3).
- **Specification — `ISpecification<T>` 0.5 SP (US-GEN-SPEC-001)** — `src/Footing.Framework/Specification/ISpecification.cs` `ISpecification<T> + Predicate/And/Or/Not + SpecificationExtensions And/Or/Where/Not` composable via `IsSatisfiedBy`, 0 deps, 11→11 regex, 3 testes Spec (And/Or/Where).
- **Benchmark — `BenchmarkSmokeTests` 0.5 SP (US-GEN-BENCHMARK-001)** — `tests/Footing.Framework.Tests/BenchmarkSmokeTests.cs` 3 testes minimal Stopwatch 10k renders <2000ms + `tests/Footing.Framework.Benchmarks/Benchmarks.cs` suite (BenchmarkDotNet opcional PrivateAssets, Stopwatch fallback), agnóstico, só `tests/`.

### Changed

- **Packaging — `Version 3.1.0 → 3.2.0` minor** — `Footing.Framework.csproj:Version` `3.1.0 → 3.2.0` minor SemVer additive sem breaking (DiSmoke+Spec+Benchmark Must puxados 4.0). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.2.0.nupkg + .snupkg ~66K` com `README+LICENSE+icon.png` (`unzip -l`), `TWE true` 0 erro, `2449→2480 LOC <3.5K`, `11→11 regex` sem novo `GeneratedRegex`, `364 Passed`.

### Notas

- `v3.1.0` 356 testes, 11 regex, pack 63K → `v3.2.0` 364 testes (+8: DiSmoke 2 + Spec 3 + Benchmark 3), 11→11 regex, pack 66K, 2480 LOC <3.5K tese, `Migrations/` não existe (fica Sprint 3.3 solo 0.5-1 SP detalhada), agnóstico 0 `Npgsql/Redis/Firebird` (ver `MIGRATION.md §8.11` + `print-30.md` espelho `issues/SPRINT-3.2-sem-migrations.md` + `issues/SPRINT-3.3-migrations-solo.md`). Tag `v3.2.0` só após auditor APROVADO ≥9.0.
- Links: `[3.2.0]: https://github.com/etelhado/Footing.Framework/compare/v3.1.0...v3.2.0` — manter `[3.1.0]` histórico.

---

## [3.1.0] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.10 — `3.0.0 → 3.1.0` minor additive sem breaking (Must 3.1.0 4.5 SP) + **Veto Final 1.6 SP §8.10.1** (Result/Pagination/Audit Won't, SPI agnóstico).

### Added

- **Diagnostics — `FootingActivitySource` OTel 0.5 SP (US-GEN-OTEL-001)** — `src/Footing.Framework/Diagnostics/FootingActivitySource.cs` `ActivitySource("Footing.Framework")` para `SqlTemplate.Render` + `EventBus.Publish` + `SqlBatch` com `tags`, guard `HasListeners`.
- **Health — `FootingHealthCheck` 0.25 SP (US-GEN-HEALTH-001)** — `src/Footing.Framework/Health/FootingHealthCheck.cs` `DbHealthCheck` + `EventBusHealthCheck` `IHealthCheck` + `AddFootingHealthChecks()` extension.
- **Idempotency — `IIdempotencyStore SPI` 0.1 SP (US-GEN-IDEMPOTENCY-001 veto-final)** — `src/Footing.Framework/Idempotency/Idempotency.cs` só `IIdempotencyStore` interface `4 LOC` `0 deps` agnóstico (Memory/Redis/Firebird/dbf exemplo docs, sem `MemoryIdempotencyStore` grudado, sem `Microsoft.Extensions.Caching.Memory`).
- **Outbox — `IOutboxStore + OutboxProcessor SPI` 0.5 SP (US-GEN-OUTBOX-001 veto-final)** — `src/Footing.Framework/Outbox/Outbox.cs` `OutboxMessage` + `IOutboxStore` + `SaveEventAsync<T>` + `OutboxProcessor : BackgroundService` poll `2s` `Batch 100` agnóstico (app fornece `Npgsql/Firebird/OleDb` impl, `InMemory` só teste/docs, `0 Npgsql` deps).

### Removed — Veto Final 1.6 SP (sem bump Version 3.1.0)

- **Common — `Result<T>` Won't definitivo 0 SP (US-GEN-RESULT-001 veto)** — `src/Footing.Framework/Common/Result.cs` `7 LOC` deletado. Stakeholder veto: "recurso de linguagem, não framework". App usa `exception + ProblemDetails` ou copia `FluentResults/LanguageExt`.
- **Data — `PagedResult<T> + QueryPagedAsync` Won't total 0 SP (US-GEN-PAGINATION-001 veto)** — `src/Footing.Framework/Data/Pagination.cs` `5 LOC` deletado. Motivo: precisa `dialect` (`LIMIT` vs `TOP/FETCH` vs `FIRST SKIP` vs `dbf TOP`) — framework agnóstico não deve escolher dialect.
- **Data — `IAuditable / ISoftDelete` Won't 3.1.1 0 SP (US-GEN-AUDIT-001 veto)** — `src/Footing.Framework/Data/Audit.cs` `5 LOC` deletado (snippet docs `Could 0.1` futuro). Impõe `DateTime+string` incompatível `Firebird/dbf`.

### Changed — Veto Final 1.6 SP (sem bump Version 3.1.0)

- **Packaging — `Microsoft.Extensions.Caching.Memory` removido** — `Footing.Framework.csproj` remove `PackageReference Caching.Memory 10.0.0` (só era para `MemoryIdempotencyStore` grudado). `Version` mantém `3.1.0` sem bump (veto `1.6 SP` sem bump).

### Changed

- **Packaging — `Version 3.0.0 → 3.1.0` minor** — `Footing.Framework.csproj:Version` `3.0.0 → 3.1.0` minor SemVer additive opt-in sem breaking. `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.1.0.nupkg + .snupkg ~70K` com `README+LICENSE+icon.png` (`unzip -l`), `TWE true` 0 erro, `2428→2478 LOC <3.5K`, `11→11 regex` sem novo regex.

### Notas

- `v3.0.0` 343 testes, 11 regex, pack 60K → `v3.1.0` 370 testes, 11 regex, pack 70K, 2478 LOC <3.5K tese (Must 4.5 SP additive).
- **Veto Final 1.6 SP (sem bump Version 3.1.0)** — `Result Won't 0 + PagedResult Won't 0 + Audit Won't 0 + Idempotency SPI 0.1 + Outbox SPI 0.5 + OTel 0.5 + Health 0.25 + Docs 0.25 = 1.6 SP (−64% vs 4.5)`. Pós-veto: `2478→2449 LOC (−29)` (<3.5K), `370→356 testes (−14 Won't)`, `pack 70K→63K` (70→68K previsto), `11 regex` mantido, `Version 3.1.0` sem bump, `0 Npgsql/Firebird/OleDb/Redis` deps, `grep Result/PagedResult/IAuditable ==0` em `src/`. Ver `MIGRATION.md §8.10.1` + `print-28.md` (espelho `issues/US-GENERAL-3.1.0-veto-final.md`). Tag `v3.1.0` só após auditor APROVADO ≥9.0.
- Links: `[3.1.0]: https://github.com/etelhado/Footing.Framework/compare/v3.0.0...v3.1.0` — manter `[3.0.0]` histórico.

---

## [3.0.0] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.9 — `2.4.0 → 3.0.0` major breaking (remove Obsolete).

### Removed — BREAKING

- **Data — `TypeHandlerRegistry` removido (US-CLEAN-REMOVE-OBSOLETE-001)** — `src/Footing.Framework/Data/TypeHandler.cs:29-50` `TypeHandlerRegistry` (`[Obsolete]` desde `0.3.0-alpha`) **deletado** — migrar para `SqlMapper.AddTypeHandler(...)` Dapper nativo. `grep -rn TypeHandlerRegistry --include="*.cs" src/` ==0 (só `CHANGELOG`/`MIGRATION` histórico). Consumers com `TypeHandlerRegistry.Register(...)` → compile error `CS0246` → `SqlMapper.AddTypeHandler(new MeuHandler())`.
- **DI — `AddDependencyWalkAuto` removido (US-CLEAN-REMOVE-OBSOLETE-001)** — `src/Footing.Framework/DI/DependencyWalkExtension.cs:10` `AddDependencyWalkAuto` (`[Obsolete]` desde `0.3.0-alpha`) **deletado** — migrar para `services.AddDependencyWalk("MeuApp", cfg, typeof(Program).Assembly)`. `grep -rn AddDependencyWalkAuto --include="*.cs" src/` ==0. Consumers com `services.AddDependencyWalkAuto("MeuApp", cfg)` → `CS0117` → `AddDependencyWalk` com assemblies explícitos.

### Changed

- **Qualidade — `TreatWarningsAsErrors` strict (US-QUAL-TWE-STRICT-001)** — `src/Footing.Framework/Footing.Framework.csproj:8-9` remove `<WarningsNotAsErrors>CS0618</WarningsNotAsErrors>`, mantém `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` strict, `dotnet build -c Release` 0 erro (só SourceLink 2 warnings externos). `tests/Footing.Framework.Tests.csproj` limpa `CS0618` de `NoWarn`.

### Notas

- `v2.4.0` 345 testes, 11 regex, pack 60K → `v3.0.0` ~343 testes (345 -2 Obsolete), `11→11 GeneratedRegex`, `TWE true` strict sem `WarningsNotAsErrors`. **Breaking** só delete `Obsolete` (SemVer major §8). Consumidores com `TypeHandlerRegistry`/`AddDependencyWalkAuto` devem migrar (ver `MIGRATION.md §8.9`). `Version 2.4.0 → 3.0.0` major SemVer sem sufixo. `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.0.0.nupkg + .snupkg ~60K` com `README+LICENSE+icon.png` sem `NU5048`. Tag `v3.0.0` só após auditor APROVADO ≥9.0.
- Links: `[3.0.0]: https://github.com/etelhado/Footing.Framework/compare/v2.4.0...v3.0.0` — manter `[2.4.0]`, `[2.3.1]`, `[2.3.0]` histórico.

---

## [2.4.0] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.8 — `2.3.1 → 2.4.0` minor sem breaking fora fail-fast trailing dot.

### Added

- **Data — `SqlTemplate` `IFDEFINED` 1 Must (US-TEMPLATE-IFDEFINED-001)** — `src/Footing.Framework/Data/SqlTemplate.cs` adiciona `[GeneratedRegex(@"\{IF(?:NOT)?DEFINED:(\w+)\}(.*?)\{END\}")]` + `RenderIfDefinedBlocks` após `WHERE` antes de `IF` + helper `IsDefined` via `ContainsKey` distingue `missing` (false) vs `null`/`""`/`Y` (true) + branch `defined(Param)` dentro de `EvaluateSingle` (`^defined\s*\(\s*(\w+)\s*\)$` → `ContainsKey`) + `ValidateTemplate` para `IFDEFINED`/`IFNOTDEFINED` → compat `and` (`FindAndIndicesOutsideQuotes` já trata `defined` como factor) + `CHOOSE WHEN defined`. Total `10→11 GeneratedRegex` (1 nova, `IFNOTDEFINED` via mesma regex `IF(?:NOT)?DEFINED`). Pipeline `BIND→INCLUDE→TRIM/SET→WHERE→IFDEFINED→IF(and+defined+length)→IN→CHOOSE→BETWEEN`. Tests: `SqlTemplateIfDefinedTests.cs` 7+ testes `MissingFalse`/`NullTrue`/`EmptyTrue`/`YTrue`/`DictMissing`/`DictNull`/`AndWithDefined`/`IfNotDefined`/`ChooseWhenDefined`. **(US-TEMPLATE-IFDEFINED-001, 1 SP)**

- **Test — `FsCheck 200 random` 1 Should (US-TEST-FSCHECK-001)** — `tests/Footing.Framework.Tests/SqlTemplateFsCheckTests.cs` 5 properties ×40 =200 random `and/length/BETWEEN/INCLUDE/Batch/ValidateTableName` com `FsCheck.Xunit 2.16.6` (`Prop_And_ShouldNeverThrow`, `Prop_Where_Trim_ShouldNeverContain_WhereAnd`, `Prop_Batch_ValidateTableName_ShouldFailFast`, `Prop_Include_ShouldNotStackOverflow`, `Prop_Bind_ShouldNotInject`), `<2s`, determinístico, `Render` nunca throw. **(US-TEST-FSCHECK-001, 1 SP)**

### Changed

- **Perf — `PropsCache` compartilhado 0.5 Should (US-PERF-PROPSCACHE-001)** — novo `src/Footing.Framework/Data/ReflectionCache.cs` `internal static ReflectionCache` + `PropsCacheHelper` alias com single `ConcurrentDictionary<Type,PropertyInfo[]> PropsCache` via `GetOrAdd` + `BindingFlags.Public|Instance CanRead` intacto, usado por `SqlTemplate.ToDictionary` e `SqlBatch.GetProperties`, thread-safe `Parallel.For`. `grep -rn PropsCache src/` → 1 definição shared (não 2 duplicadas). **(US-PERF-PROPSCACHE-001, 0.5 SP)**

- **Data — `SqlBatch.ValidateTableName` trailing dot 0.25 Must (US-DATA-BATCH-SEC-002)** — `src/Footing.Framework/Data/SqlBatch.cs:163` regex `^[A-Za-z_][A-Za-z0-9_\.]*$` → `^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$` rejeita `Users.`/`schema..Users`/`.Users`/`Users; DROP`/`123Users`, pass `Users`/`schema.Users`. **Breaking leve** `Users.` antes passava → depois `throw ArgumentException` (uso legítimo nunca tem trailing dot). Tests: `SqlBatchSecTests.cs` +3 trailing dot. **(US-DATA-BATCH-SEC-002, 0.25 SP)**

- **Packaging — `Version 2.3.1 → 2.4.0` minor** — `Footing.Framework.csproj:Version` `2.3.1 → 2.4.0` minor SemVer (additive IFDEFINED opt-in, sem breaking fora trailing dot fail-fast). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.4.0.nupkg + .snupkg ~60K` com `README+LICENSE+icon.png` sem `NU5048`. **(Sprint 09 must have)**

### Notas

- `v2.3.1` 327 testes, 10 regex, pack 59K → `v2.4.0` ~345 testes (327+7+3+5 plus extra IFDEFINED/CHOOSE), `10→11 GeneratedRegex`, `SqlTemplate.cs ~943L` + `ReflectionCache.cs` + `SqlBatch 175L`, `FindAndIndicesOutsideQuotes` and simples intacto, `SqlTemplateParseException` intacto, `TWE true`, `Deterministic true`. **Sem breaking fora trailing dot fail-fast** (`Users.` pass→throw). Docs `MIGRATION §8.8`, `PUBLISH 2.4.0`, `README 10 tags` (+IFDEFINED). Tag `v2.4.0` só após auditor APROVADO ≥9.0.
- Links: `[2.4.0]: https://github.com/etelhado/Footing.Framework/compare/v2.3.1...v2.4.0` — manter `[2.3.1]`, `[2.3.0]` histórico.

---

## [2.3.1] - 2026-09-06

> **Migration Guide:** sem breaking vs `2.3.0` — patch docs only (pack size + git remote).

### Fixed

- **Docs — pack size 51K→59K** — `CHANGELOG [2.3.0] Notas` e `MIGRATION §8.6` diziam `~51K (49–55K)` mas `artifacts/Footing.Framework.2.3.0.nupkg` real `59K` (180679 bytes unzip, `README+LICENSE+icon.png` via `unzip -l`). Corrigido para `~59K` nesta `2.3.1`. `PUBLISH.md` já bump `2.3.0` parcial — agora `2.3.1` coerente.

### Changed

- **Packaging — `Version 2.3.0 → 2.3.1` patch** — `Footing.Framework.csproj:Version` `2.3.0 → 2.3.1` patch SemVer docs-only. `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.3.1.nupkg + .snupkg ~59K` com `README+LICENSE+icon.png` (`unzip -l *.nupkg | grep icon.png`), sem `NU5048`, `Deterministic true`, `PublishRepositoryUrl true`.

### Notas

- Sem código vs `2.3.0` — 327 testes, 10 `GeneratedRegex`, `SqlTemplate.cs 905L` + `SqlBatch 176L ValidateTableName` intactos, `FindAndIndicesOutsideQuotes` and simples intacto, `SqlTemplateParseException` intacto. **Sem breaking** `2.3.0 → 2.3.1` — patch docs. Tag `v2.3.1` só após auditor APROVADO ≥9.0.
- Links: `[2.3.1]: https://github.com/etelhado/Footing.Framework/compare/v2.3.0...v2.3.1` — manter `[2.3.0]` histórico.

---

## [2.3.0] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.6 — `2.2.0 → 2.3.0` stable fechamento (and + fail-fast + gap MyBatis viável, 4.0 SP).

### Added

- **Data — `SqlTemplate` `and` simples 0.5 SP (US-TEMPLATE-AND-001, already done `v2.3.0-alpha` c577df0/d8fdd4c)** — `{IF: A and B}` só `and` com 1× `and`, 2 fatores, case-insensitive fora de `''/""`, short-circuit, compat `length()`, rejeita `or/()/!/a and b and c` com `false+Warning`. Helpers `FindAndIndicesOutsideQuotes` + `ContainsOr/Parens/ExclamationOutsideQuotes`, `EvaluateCondition` delega `EvaluateSingle`. Tests: `tests/Footing.Framework.Tests/SqlTemplateAndTests.cs` 11 testes Gherkin (busca/status/length/age range, case insensitive AND, or não suportado, and 3 sem suporte, aspas, WHEN/CHOOSE, parênteses/! rejeitado). Mantém 7 `GeneratedRegex`. **(US-TEMPLATE-AND-001, 0.5 SP)** — já em `2.3.0-alpha` 314 testes.
- **Data — `SqlTemplate` gap MyBatis viável 2.5 SP (SPRINT-07)** — `src/Footing.Framework/Data/SqlTemplate.cs:16,42,330` adiciona `FragmentCache ConcurrentDictionary` + `RegisterFragment(id,sql)` + `[GeneratedRegex(@"\{(?:INCLUDE|SQL):(\w+)\}")]` + `RenderIncludeBlocks` com depth 5 guard + miss literal; `TrimContent(prefix,prefixOverrides,suffix,suffixOverrides)` genérico + `[GeneratedRegex(@"\\{TRIM...\\}...\\{ENDTRIM\\}")]` + `SetRegexManual` açúcar `TrimContent(prefix:"SET", suffixOverrides:",")` + refatora `{WHERE}` delega `TrimContent(prefix:"WHERE", prefixOverrides:"AND|OR ")`; `[GeneratedRegex(@"\{BIND:(\w+),\s*value=(.+?)\}")]` + `RenderBindBlocks` primeiro no pipeline injeta `paramDict[Var]=concat('literal'+param)`, valida `Var ^[A-Za-z_]\w*$`, rejeita `;--/*`. Pipeline final `Bind→Include→Trim/Set→Where→If(and)→In→Choose→Between→Cleanup`. Total `7→10 GeneratedRegex` (INCLUDE+TRIM+BIND; SET delega sem regex extra). Tests: `SqlTemplateIncludeTests` 2 testes + `SqlTemplateTrimSetTests` 4 testes + `SqlTemplateBindTests` 2 testes. **(US-GAP-INCL-001 1 Must + US-GAP-TRIM-SET-001 1 Should + US-GAP-BIND-001 0.5 Should)**

### Changed

- **Data — `SqlTemplate` fail-fast `SqlTemplateParseException` S1-S6 1 Must (US-TEMPLATE-FAILFAST-001, 1 SP)** — novo `src/Footing.Framework/Data/SqlTemplateParseException.cs` `sealed class SqlTemplateParseException : InvalidOperationException` com `Tag/Expected/Line/Column/Snippet/Suggestion`; `SqlTemplate.Parse` e `RenderTemplate` guard chamam `ValidateTemplate(string sql)` ~60 LOC Stack LIFO `{IF→END}`/`{WHERE→ENDWHERE}`/`{CHOOSE→ENDCHOOSE}`/`{WHEN→ENDWHEN}`/`{BETWEEN→END}`/`{TRIM→ENDTRIM}`/`{SET→ENDSET}`, `Unclosed {` scan com `IndexOf('}')` + `next '{' < '}'` detect, line/col via `\n` count + `LastIndexOf('\n')`, snippet 60 chars, suggestion `add '{END}'`. Bubble `SqlTemplateLoader.Load` não guarda Cache cagado. Reescreve `tests/Footing.Framework.Tests/Helpers/ChaosData.cs` C1 6 rows + `SqlTemplateChaosTests.cs` C1 4 theories `Contains(literal)` → `Throws<SqlTemplateParseException>` com `Line>0/Tag/Expected`; adiciona `Parse_MissingEnd_ThrowsWithLineCol` + `Loader_Malformed_Throws`. Mantém 7→7 regex nesta etapa (validação usa existentes). **Breaking alpha permitido** `C1 literal→throw` (v2.2.0-alpha→2.3.0). **(US-TEMPLATE-FAILFAST-001)**
- **Packaging — `Version 2.3.0 stable`** — `Footing.Framework.csproj:Version` `2.3.0-alpha` → `2.3.0` stable (SemVer, sem sufixo). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.3.0.nupkg` + `.snupkg` ~51K com `README.md`+`LICENSE`+`icon.png` (`unzip -l *.nupkg | grep icon.png`), sem `NU5048`, `Deterministic true`, `PublishRepositoryUrl true`. **(SPRINT-07 must have, 0.5 SP)**

### Notas

- `2.3.0-alpha` pre-release d8fdd4c (314 testes, 7 regex, and simples 0.5 SP done, stakeholder “pode seguir”) contida nesta `2.3.0` — sem diff de `and` além de pack stable.
- `v2.2.0 stable` 10/10 (e4673df, 303 testes) já tagueada; `v2.3.0-alpha` pre-release (d8fdd4c) histórico preservado.
- Total SPRINT-07 fechamento 3.5 SP restante + 0.5 and done = **4.0 SP** (`FAILFAST 1 Must` + `INCLUDE 1 Must` + `TRIM/SET 1 Should` + `BIND 0.5 Should` + docs/pack/tag). `and simples` já done 0.5 SP (print-18) — esta `2.3.0` fecha `v2.3.0 stable` ~327 testes (314→327, mínimo 324), `7→10 GeneratedRegex`, `pack ~51K` (49–55K range). **Sem breaking fora fail-fast C1** (`0.1.0-alpha → 2.3.0` histórico completo em `MIGRATION.md §8.6`).
- Links: `[2.3.0]: https://github.com/etelhado/Footing.Framework/compare/v2.2.0...v2.3.0` — manter `[2.2.0]`, `[2.3.0-alpha]` histórico.

---

## [2.2.0] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.5 — `2.1.0 → 2.2.0` sem breaking só promoção (chaos já em alpha).

### Changed

- **Packaging — `Version 2.2.0 stable`** — `Footing.Framework.csproj:Version` `2.2.0-alpha` → `2.2.0` stable (SemVer, sem sufixo). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.2.0.nupkg` + `.snupkg` ~50K com `README.md`+`LICENSE`+`icon.png` (`unzip -l *.nupkg | grep icon.png`), sem `NU5048`. **(SPRINT-06 must have, 0.25 SP)**

### Notas

- Sem código vs `2.2.0-alpha` — 303 testes, 7 `GeneratedRegex`, `EvaluateCondition` + `TryToDateTime` + `IsOrderingComparable` + `EvaluateLengthParam` mantidos, `D size` automático rejeitado (`grep Length.CompareTo ==0`, `Name>2` false), `SqlBatch` 176L `ValidateTableName` intacto, `BETWEEN` flat intacto, fail-safe C1 literal mantido (esquecer `{END}` → literal visível, deferido fail-fast 1 SP para `2.3.0`), docs tri-state + gap deferido. **Sem breaking change** `2.2.0-alpha → 2.2.0` — apenas `Version` stable para `PackageReference` sem `-alpha`. Tags `v1.0.0`/`v2.0.0`/`v2.1.0`/`v2.2.0-alpha` preservadas → `v2.2.0`.
- **Resposta ressalva auditor 9.2/10:** docs pendentes `CHANGELOG/MIGRATION/README` sanados nesta promoção; `fail-fast S1-S6 throw`, `and simples`, `IFDEFINED` replanejados para `2.3.0` (ver `print-17.md` backlog 2.3 + `print-16.md` deferido).
- Links: `[2.2.0]: https://github.com/etelhado/Footing.Framework/compare/v2.1.0...v2.2.0` — manter `[2.2.0-alpha]` histórico.

---

## [2.1.0] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.3 — `2.0.0 → 2.1.0` additive sem breaking (`length()` opt-in, `PackageIcon`, `TreatWarningsAsErrors`, `tableName` guard). Sem breaking, `Version 2.1.0` minor.

### Added

- **Data — `SqlTemplate` `length(Param) > N` explícito opt-in (US-TEMPLATE-LEN-001, 1 SP)** — `src/Footing.Framework/Data/SqlTemplate.cs:204-342` adiciona branch `length()` no início de `EvaluateCondition` (`^length\s*\(\s*(\w+)\s*\)\s*(==|!=|<>|>=|<=|>|<|=|gte|lte|gt|lt|ge|le)\s*(.+)$` IgnoreCase) + helper `EvaluateLengthParam(string paramName, dict) → int` (`null/missing → 0`, `string → s.Length`, `não-string → ToString().Length`), parse `rightRaw` idêntico ao existente (`null|'str'|número|bool|@Param|bare`), normaliza op (`=→==`, `<>→!=`, `gt→>` etc.), `null` com `length` retorna `==` false/`!=` true/`> < >= <=` false, `IsOrderingComparable(decimal, rightVal)` antes de `> < >= <=` com `Debug.WriteLine Warning`. Mantém 7 `GeneratedRegex`, `C Name>2 false` intacto, `string vs string Ordinal`, `TryToDateTime`, `null handling`. Ex: `{IF:length(Name) > 18} AND Name=@Name {END}` (30 chars → contém, 4 chars → omitido), `{IF:length(Name) == 0}` para empty, `{IF:length(Name) > @Min}` com `@Min=2`. **Additive opt-in** — `Name>2` mantém `false+Warning` (prova `length(Name)>2 true` vs `Name>2 false`). Tests: `tests/Footing.Framework.Tests/SqlTemplateV3LengthTests.cs` 7 testes, `dotnet test` 71→81 passed. **(US-TEMPLATE-LEN-001)**
- **Packaging — `PackageIcon` 128x128 (US-PACK-ICON-001, 0.5 SP)** — novo `icon.png` 128×128 PNG na raiz + `Footing.Framework.csproj: <PackageIcon>icon.png</PackageIcon>` + `<None Include="..\..\icon.png" Pack="true" PackagePath="\" />`, `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.1.0.nupkg` + `.snupkg` ~49K com `README.md`+`LICENSE`+`icon.png` (`unzip -l *.nupkg | grep icon.png`), sem `NU5048`. **(US-PACK-ICON-001)**

### Fixed

- **Data — `SqlBatch` `tableName` quoting guard (US-DATA-BATCH-SEC-001, 0.5 SP)** — `src/Footing.Framework/Data/SqlBatch.cs:14,28,67,111,161` adiciona `ValidateTableName(string tableName)` com `Regex.IsMatch(@"^[A-Za-z_][A-Za-z0-9_\.]*$")` → `throw ArgumentException("tableName contains invalid characters; allowed: A-Z, 0-9, _, .", nameof(tableName))`, chamado no início de `BuildBatchInsert`/`InsertBatchAsync` (3 overloads + `UnitOfWork` via delegação). Fail-fast para `Users; DROP`/`a; b` injection, pass para `Users`/`schema.Users`/`Users_123`. Tests: `tests/Footing.Framework.Tests/SqlBatchSecTests.cs` 3 testes. **(US-DATA-BATCH-SEC-001)**

### Changed

- **Qualidade — `TreatWarningsAsErrors=true` + `WarningsNotAsErrors CS0618` (US-QUAL-TWE-001, 1 SP)** — `src/Footing.Framework/Footing.Framework.csproj:8-9` `false → true` + `<WarningsNotAsErrors>CS0618</WarningsNotAsErrors>` para permitir `[Obsolete] TypeHandlerRegistry`/`AddDependencyWalkAuto` até `3.0.0` removal sem quebrar build, `NoWarn CS1591` mantido, `ContinuousIntegrationBuild false` já mitiga `SourceLink` `MSB` warnings. `dotnet build -c Release` 2 warnings só SourceLink (externo) + 0 erro, `dotnet test 81 passed` com `TWE true`. `tests/Footing.Framework.Tests.csproj` adiciona `NoWarn CS0618;CS8765;xUnit1031;xUnit2009` para 0 warning. **(US-QUAL-TWE-001)**
- **Packaging — `Version 2.0.0 → 2.1.0`** — `Footing.Framework.csproj:Version` `2.0.0 → 2.1.0` minor SemVer (additive, sem `-alpha`, opt-in `length()`). `dotnet pack 2.1.0` ~49K. **(US-DOC-004)**

### Notas

- **Sem breaking change** `2.0.0 → 2.1.0` — `length()` é novidade additive opt-in (`{IF:Name > 18}` mantém `false+Warning` C, só `{IF:length(Name) > 18}` habilita `decimal` Length); `PackageIcon`/`TWE`/`tableName` não quebram templates `2.0.0`; consumidores `Academia` migram `Version="2.0.0"` → `Version="2.1.0"` sem código (ver `MIGRATION.md §8.3`). Tags `v1.0.0`/`v2.0.0-alpha`/`v2.0.0-alpha.1`/`v2.0.0` preservadas → `v2.1.0`. Could Have `2.2.0` (`BenchmarkDotNet`/`PropsCache shared`) e Won't `3.0.0` (`and/or/!/()` + remove Obsolete) permanecem futuros.
- **Docs — tri-state `null / Y / N` traz todos + `IS NULL` (US-TEMPLATE-tri-state-null, 0.5 SP docs sem código, pós-14f5d7d)** — `README.md` e `src/Footing.Framework/README.md` § Tri-state com tabela T1 traz-todos (`{WHERE} {IF:Ativo} AND c.Ativo=@Ativo {END} {ENDWHERE}` → `missing/null/"" → traz todos`, `Y/N → filtra`) + T3 IS NULL (`{IF:Ativo == null} AND c.Ativo IS NULL` → `missing` e `null` hoje indistinguíveis BC `new {} == null true`) + 4 estados Y/N/IS NULL/Todos com 2 params (`Ativo` + `BuscarNulos==true`) + nota `and` implícito (`{IF:Ativo}` já é `TryGetValue && !=null && !IsNullOrEmpty` = `!= null and != ''` shorthand, `{IF:Ativo != null}` vaza `""`) + `MIGRATION.md §8.4` com backlog `and/or/!/()` Won't 3.0 gatilho BUY >30% (ver `print-13.md`) e `IFDEFINED:Ativo` Could Have 2.2.0 1 SP 7→8 regex (`ContainsKey` distingue `missing` vs `null`) não implementado em `v2.1.0`; `print-14.md` espelho git de `issues/US-TEMPLATE-tri-state-null.md` commitado; `SqlTemplate.cs`/`SqlBatch.cs`/tests **não tocados** (`grep GeneratedRegex ==7`, 81 passed). **(US-TEMPLATE-tri-state-null docs)**
- Links: `[2.1.0]: https://github.com/etelhado/Footing.Framework/compare/v2.0.0...v2.1.0`.

---

## [2.0.0] - 2026-09-06

> **Migration Guide:** sem breaking vs `2.0.0-alpha.1` — apenas promoção stable (ver [`MIGRATION.md`](./MIGRATION.md) §8.1 → `2.0.0`).

### Changed

- **Packaging — `Version 2.0.0 stable`** — `Footing.Framework.csproj:Version` `2.0.0-alpha.1` → `2.0.0` stable (SemVer, sem sufixo). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.0.0.nupkg` + `.snupkg` ~49K com `README.md` + `LICENSE`. `Description` sem `sqlbuilder`, `PackageTags` sem `sqlbuilder`, `PublishRepositoryUrl`/`Deterministic` mantidos. Sem código novo — promoção pura. **(US-PACK-003 SPRINT-04 must have)**

### Notas

- Sem código novo vs `2.0.0-alpha.1` — 71 testes, 7 `GeneratedRegex`, `EvaluateCondition` 80L + `TryToDateTime` + `IsOrderingComparable` mantidos, `D size` automático rejeitado (`grep Length.CompareTo ==0`, `Name>2` false), `SqlBatch` 166L intacto, `BETWEEN` flat intacto, sem `throw` (fail-safe omitido `false+Warning`). **Sem breaking change** `2.0.0-alpha.1 → 2.0.0` — apenas `Version` stable para `PackageReference` sem `-alpha`. Tags `v1.0.0`/`v1.1.0-alpha` descartado/`v2.0.0-alpha`/`v2.0.0-alpha.1` → `v2.0.0` histórico preservado. `PackageIcon`/`TreatWarningsAsErrors`/`Benchmark` permanecem Could Have `2.1.0` (ver `MIGRATION.md` §8.1 e `CHANGELOG [2.0.0-alpha.1] Notas`). **(US-DOC-003)**
- Links: `[2.0.0]: https://github.com/etelhado/Footing.Framework/compare/v2.0.0-alpha.1...v2.0.0` — manter `[2.0.0-alpha.1]` e `[2.0.0-alpha]` abaixo como histórico.

---

## [2.0.0-alpha.1] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8.1 — hotfix comparador não-numérico (US-TEMPLATE-v3-comparador-nao-numerico + D-size rejeitado).

### Fixed

- **Data — `SqlTemplate` comparador `> < >= <=` com não-numérico — falso+Warning (Opção C, 0.5 SP)** — `src/Footing.Framework/Data/SqlTemplate.cs:204-330` atualiza `EvaluateCondition` + `Compare`: adiciona `TryToDateTime` (DateTime/DateTimeOffset/string Invariant+CurrentCulture), prioriza `string vs string Ordinal` antes de `IComparable`, valida `IsOrderingComparable` antes de `> < >= <=` → se `string vs número` (`"abc" >100`) ou tipos incompatíveis retorna `false` + `Debug.WriteLine Warning` (sem throw, fail-safe omitido), corrige `null >= null` → `false` (era `true`, agora só `==` true, alinhado SQL UNKNOWN). Mantém shorthand `{IF:Status}`, `== !=` com `TryToDecimal/string Ordinal`, `BETWEEN` flat intacto, `SqlBatch` 166L intacto, 7 `GeneratedRegex`. **Antes:** `"abc" >100 → true` (Ordinal `a97>149`) bug silencioso. **Depois:** `false+Warning`. `"eder">"adam"` mantém `true` Ordinal, `"2024-12-31" > "2024-01-01"` cronológico via `TryToDateTime`, `DateTime via @Param` cronológico. **Breaking:** leve `true→false` só para bug; stakeholder Sueli aprovou C, D size automático rejeitado. Gates: `grep TryToDateTime` presente, `grep Length.CompareTo ==0` (D não implementado), `grep -c GeneratedRegex ==7`. **(US-TEMPLATE-v3-comparador-nao-numerico, US-TEMPLATE-v3-comparador-D-size)**
- **Tests — 3 novos testes comparador C (71 total)** — novo `tests/Footing.Framework.Tests/SqlTemplateV3ComparadorTests.cs` com 3 testes: `StringVsNumero_RetornaFalse` (`"abc" >100 false`, `"abc" >2 false prova D rejeitado, `@Limite`, numérico mantém true), `StringVsString_MantemOrdinal` (`"eder">"adam" true`, `"adam">"eder" false`, `@Outro`, acento Ordinal), `DataString_E_DateTime_Cronologico_ENull` (`"2024-12-31" > "2024-01-01" cronológico`, `DateTime via @Param`, misto `DateTime vs '2024-01-01'`, `null >= null false`, `BETWEEN` intacto). `dotnet test -c Release` 71 passed (68+3). **(US-TEMPLATE-v3-comparador-nao-numerico)**
- **Packaging — `Version 2.0.0-alpha.1`** — `Footing.Framework.csproj:Version` `2.0.0-alpha` → `2.0.0-alpha.1` hotfix C. `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.0.0-alpha.1.nupkg` + `.snupkg` com `README`+`LICENSE`. **(US-TEMPLATE-v3-comparador-nao-numerico)**

### Changed

- `Footing.Framework.csproj:Version` `2.0.0-alpha` → `2.0.0-alpha.1` (hotfix 0.5 SP, Opção C).

---

## [2.0.0-alpha] - 2026-09-06

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §8 — `1.1.0-alpha` bug DESCARTADO → `2.0.0-alpha` breaking clean (US-TEMPLATE-v3, 5 SP).

### ⚠️ Breaking Changes

- **Data — `SqlTemplate` v3 breaking clean — remove 11 tags, unifica em `{IF: expr}` + `{WHEN: expr}` (US-TEMPLATE-v3, 5 SP)** — `src/Footing.Framework/Data/SqlTemplate.cs` reescrito: **deletados** 11 `GeneratedRegex` + 11 métodos `Render*Blocks` bugados (`EQ/NE/LT/GT/GTE/GE/LTE/LE/BETWEEN?` + `IFNOTNULL/IFNOTEMPTY` redundantes); **mantidos** 7 regex (`IF([^}]+)`, `WHERE`, `CHOOSE`, `WHEN([^}]+)`, `OTHERWISE`, `IN`, `BETWEEN`); **novo** `EvaluateCondition(string expr, Dictionary<string,object?>)` 80 LOC com gramática `Param | Param Operator Operand` (`==|!=|=|<>|>|<|>=|<=|gt|lt|gte|lte|ge|le` case-insensitive, operandos `null|'str'|"str"|número|bool|@Param|bare Param`), helpers `AreEqual` (decimal para numérico, Ordinal para string), `Compare` (decimal/IComparable), `IsNumeric`, `TryToDecimal`; shorthand `{IF:Status}` existência; `RenderIfBlocks`/`RenderChooseBlocks` usam `EvaluateCondition`; pipeline enxuto `Where→If→In→Choose→Between→Cleanup` (5 passos, não 10). LOC 433→~350 (-80). **Gates:** `grep -c GeneratedRegex ==7`, `grep EqRegex ==0`, `grep IfNotNullRegex ==0`. **(US-TEMPLATE-v3)**
- **Data — `SqlTemplate` `{EQ}→{IF:Status}` e `{IFNOTNULL}→{IF:Status != null}` etc. — migração obrigatória** — templates `.sql` com `{EQ}/{NE}/{LT}/{GT}/{GTE}/{GE}/{LTE}/{LE}/{IFNOTNULL}/{IFNOTEMPTY}` permanecem literais (fail-safe) e devem ser migrados para `{IF:Status}`, `{IF:Status == 'ACTIVE'}`, `{IF:Preco > 100}`, `{IF:Status != null}`, `{IF:Status != ''}`. Ver `MIGRATION.md` §8 tabela completa. **(US-TEMPLATE-v3)**
- **Docs — `README.md` tabela Tags reescrita para 5 tags** (`IF:expr`, `WHERE`, `IN`, `CHOOSE`, `BETWEEN`) + seção Removidos v2.0.0-alpha. **(US-TEMPLATE-v3)**

### Added

- **Tests — 15 novos testes Gherkin v3 (68 total)** — novo `tests/Footing.Framework.Tests/SqlTemplateV3Tests.cs` com 15 testes cobrindo 9 Gherkin (§8 v3): `== 'eder'`, `!= 'eder'`, `>100`/`gt|lte` aliases, `!=null/==null`, `== @Outro`/`bare`, shorthand, `WHERE` mixed, `WHEN` expr, `numeric_types`, `todos_juntos` + 4 testes de remoção (`{EQ}`/`{IFNOTNULL}`/`{IFNOTEMPTY}`/`{NE}/{LT}...` permanecem literais). `SqlTemplateTests.cs` migrado (`IFNOTNULL`→`IF != null`, `IFNOTEMPTY`→`IF != ''`/`IN`), `SqlTemplateOpsTests.cs` migrado para `IF expr` com comparação real. `dotnet test -c Release` 68 passed. **(US-TEMPLATE-v3)**
- **Packaging — `Version 2.0.0-alpha`** — `Footing.Framework.csproj:Version` `1.1.0-alpha` (bug descartado) → `2.0.0-alpha` breaking clean (sem tag ainda — auditoria @aud_carlos pendente). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.0.0-alpha.nupkg`. **(US-TEMPLATE-v3)**

### Changed

- `Footing.Framework.csproj:Version` `1.1.0-alpha` → `2.0.0-alpha` (roadmap `print-07.md` v2.0.0-alpha Must Have 5 SP, breaking livre stakeholder Sueli).
- `src/Footing.Framework/Data/SqlTemplate.cs` LOC 433→302 (aprox. -130 com remoção, +80 parser).

### Notas

- `SqlBatch` mantido intacto 166 LOC (sem breaking, chunk 500 + 2100/colCount).
- `print-07.md` versionado como espelho git de `issues/US-TEMPLATE-v3-breaking-redesign.md`.

---

## [1.1.0-alpha] - 2026-09-05 (DESCARTADO — bug CRITICAL `EQ*` existência)

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §7 — `1.0.0 → 1.1.0-alpha` sem breaking (13 SP: OPS 5 + BATCH 8).

### Added

- **Data — `SqlTemplate` operadores de comparação `{EQ}{NE}{LT}{GT}{GTE}{LTE}{BETWEEN}` (US-TEMPLATE-OPS, 5 SP)** — `src/Footing.Framework/Data/SqlTemplate.cs` ganha 9 `GeneratedRegex` (`EQ/NE/LT/GT/GTE/GE/LTE/LE/BETWEEN`) + 9 métodos `Render*Blocks` com semântica idêntica a `RenderIfBlocks` (`null`/`""`/ausente → omitido, `0`/`false` → renderiza), integrados em `RenderTemplate` e `RenderConditionsInBlock` (aninhado em `{WHERE}`). `BETWEEN` usa convenção flat `ParamMin`/`ParamMax` (`hasMin && hasMax`). Compatibilidade 1.0.0 total (regex disjuntas, templates antigos idênticos). Docs `README.md` tabela Tags + `MIGRATION.md` §7. **(US-TEMPLATE-OPS)**
- **Data — `SqlBatch` helper insert batch chunked Dapper-friendly (US-DATA-BATCH, 8 SP)** — novo `src/Footing.Framework/Data/SqlBatch.cs` (120 linhas) com `BuildBatchInsert<T>(table, items, columnsOverride?) → (Sql, DynamicParameters)` + `InsertBatchAsync<T>(IDbConnection/UnitOfWork, table, items, tx, batchSize=500, ct)` com `ConcurrentDictionary PropsCache`, `DynamicParameters` (`@p{i}_{Col}`), chunk `effective = min(500, 2100/colCount)` e `CancellationToken`, DB-agnostic (PG/MySQL/SQLite/SQLServer) sem `DataTable`/`SqlBulkCopy`. `README.md` seção `Batch Insert` + `MIGRATION.md` §7. **(US-DATA-BATCH)**
- **Tests — 24 novos testes (53 total, 29 existentes verdes)** — `SqlTemplateOpsTests.cs` (13 testes, 7 cenários Gherkin OPS) + `SqlBatchTests.cs` (11 testes, 7 cenários BATCH com `FakeConnection` chunking, null handling, column filtering, UoW, CT, Dapper compat). `dotnet test -c Release` 53 passed. **(US-TEMPLATE-OPS + US-DATA-BATCH)**
- **Packaging — `Version 1.1.0-alpha`** — `Footing.Framework.csproj:Version` `1.0.0 → 1.1.0-alpha` (SemVer pre-release para 1.1.0, sem tag ainda — auditoria pendente). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.1.1.0-alpha.nupkg` com `README`+`LICENSE`.

### Changed

- `Footing.Framework.csproj:Version` `1.0.0 → 1.1.0-alpha` (roadmap `print-04.md` v1.1.0 Must Have 13 SP).

### Notas — Checklist §8 Won't Have (mantido 1.1.0-alpha)

- `PackageIcon` / `TreatWarningsAsErrors` / `BenchmarkDotNet` permanecem Could Have 1.1.0 stable (não bloqueiam alpha).

---

## [1.0.0] - 2026-09-04

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §2-§4 e nota `0.3.0-alpha → 1.0.0` — sem breaking, apenas fixes (Sprint 03).

### Fixed

- **Docs — `README.md` índice `ColumnAttribute` removido** — `README.md:27` e `src/Footing.Framework/README.md:27` removem `- [ColumnAttribute](#columnattribute)` do índice Data Layer; seção `### ColumnAttribute` mantida com nota `> Removido em 0.3.0-alpha` (âncora não 404). `grep -n ColumnAttribute README.md` retorna apenas `~382`. **(US-DOC-001)**
- **Tests — portabilidade `AppContext.BaseDirectory` relativo** — `tests/Footing.Framework.Tests/EventBusChannelTests.cs:115-118` e `LifecycleDedupTests.cs:194` trocam hardcode `/home/etelhado/...` por `Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", ...))`. `grep -rn "/home/etelhado" --include="*.cs" tests/` == 0 (exit 1). **(US-TEST-001)**
- **DI — `DependencyWalkExtension.cs:15,24` + `ResolverExtensions.cs:27` `StringComparison.Ordinal`** — `StartsWith(rootNamespace + ".", StringComparison.Ordinal)` e `StartsWith(rootNamespace, StringComparison.Ordinal)` para semântica ordinal ASCII (`Footing.Framework` vs `Footing.FrameworkExtra`). `grep -n StringComparison.Ordinal` retorna 3 ocorrências. **(US-DI-002)**

### Changed

- `Footing.Framework.csproj:Version` `0.3.0-alpha` → `1.0.0` (SemVer stable, sem sufixo `-alpha`; `0.y.z` breaking agrupado permitido — primeiro stable). `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.1.0.0.nupkg` + `Footing.Framework.1.0.0.snupkg` com `README.md` + `LICENSE`. `Description`/`PackageTags` sem `sqlbuilder` re-validado. **(US-PACK-002)**

### Notas — Checklist §8 Won't Have (adiado para 1.1.0)

- **PackageIcon** — mantido `<PackageIcon></PackageIcon>` vazio com comentário `<!-- icon opcional -->`; sem warning `NU5048`, pack válido com `README`+`LICENSE`. Justificativa: icon 128x128 esforço baixo-valor, não bloqueia `stable`.
- **TreatWarningsAsErrors** — mantido `false`; habilitar em `1.1.0` após limpar `CS0618` residual (`TypeHandlerRegistry`/`AddDependencyWalkAuto` removidos em `2.0.0`). Habilitar agora quebraria build com warning Obsolete esperado. Justificativa: §6 Risco médio.
- **BenchmarkDotNet** — não foi criado suite formal `BenchmarkDotNet`; `Burst_10k_publish_sem_loss` já valida `10k renders <100ms` (~1s Total com `Channel`). Justificativa: validação funcional existente, benchmark formal Could Have `1.1.0`.

---

## [0.3.0-alpha] - 2026-09-04

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md) §2-§4 para `0.2.0 → 0.3.0`.

### ⚠️ Breaking Changes

- **Data — `ColumnAttribute` removido** — `src/Footing.Framework/Data/ColumnAttribute.cs` deletado; `grep -rn ColumnAttribute --include="*.cs" src/` == 0 fora de `CHANGELOG`/`MIGRATION`. Use Dapper `CustomPropertyTypeMap`, `DefaultTypeMap.MatchNamesWithUnderscores = true` ou `AS` no SQL. **(US-CLEAN-001)**

### Fixed

- **Data — `SqlTemplate.ToDictionary` com cache `ConcurrentDictionary<Type,PropertyInfo[]>` + suporte `IDictionary`/`ExpandoObject`** — `ToDictionary` agora tem `PropsCache.GetOrAdd` (reflexão 1× por Type), trata `IDictionary<string,object?>`, `IDictionary<string,object>` (inclui `ExpandoObject`) e `IDictionary` não-genérico (`Hashtable`) sem reflexão; `StringComparer.OrdinalIgnoreCase`; thread-safe (`Parallel.For` 10 threads). **(US-SQL-002)**
- **Data — `TypeHandlerRegistry` thread-safe** — `Handlers` trocado para `ConcurrentDictionary<Type,ITypeHandler>`; `Register` thread-safe. **(US-CLEAN-001)**
- **DI — `DependencyWalkExtension` scanning seguro completo** — `AddDependencyWalkAuto` agora consulta `Assembly.GetEntryAssembly()?.GetReferencedAssemblies()` + `Assembly.Load` para assemblies referenciados ainda não carregados; filtra por `GetName().Name == rootNamespace || StartsWith(rootNamespace+".")`; trata `ReflectionTypeLoadException` com `ex.Types.Where(t=>t!=null)`. **(US-DI-001)**

### Deprecated

- **`TypeHandlerRegistry`** — marcado `[Obsolete("Use SqlMapper.AddTypeHandler (Dapper) diretamente. TypeHandlerRegistry será removido em 1.0.")]`. **(US-CLEAN-001)**
- **`DependencyWalkExtension.AddDependencyWalkAuto`** — marcado `[Obsolete("Use AddDependencyWalk(rootNamespace, config, assemblies) com assemblies explícitos. AddDependencyWalkAuto será removido em 1.0.")]`. **(US-DI-001)**

### Changed

- `Footing.Framework.csproj:Version` `0.2.0-alpha` → `0.3.0-alpha`; `Description`/`PackageTags` sem `sqlbuilder` verificado; `InternalsVisibleTo Footing.Framework.Tests` para testes.
- `README.md` e `src/Footing.Framework/README.md` `Type Handlers` documenta `SqlMapper.AddTypeHandler` preferido + nota `[Obsolete]`; `ColumnAttribute` seção substituída por `> Removido em 0.3.0-alpha`.
- `MIGRATION.md` §2-§4 atualizados para refletir `ColumnAttribute` removido, `TypeHandlerRegistry` ConcurrentDictionary + Obsolete, `AddDependencyWalkAuto` Obsolete.

### Added

- **15+ testes** — 8 `SqlTemplate` B4/B6 + `ToDictionary` (`Dictionary`/`Expando`/`Hashtable`/cache/thread-safe), 3 `Channel` (10k burst sem loss, Capacity=2 backpressure, StopAsync drena), 4 `Lifecycle/dedup` (`PreDestroy async` sem deadlock, `PostConstruct` InnerException, idempotente). **(Sprint 02 DoD)**

---

## [0.2.0-alpha] - 2026-09-04

> **Migration Guide:** ver [`MIGRATION.md`](./MIGRATION.md#010-alpha--020-alpha) para passo-a-passo `< 15 min` por query.

### ⚠️ Breaking Changes

- **Data — `SqlBuilder` removido** — `src/Footing.Framework/Data/SqlBuilder.cs` deletado. `SqlTemplate` + `SqlTemplateLoader` é agora a **única** API de SQL dinâmico. Motivo: duplicação total com `SqlTemplate` (bugs B4/B6 e `ToDictionary` sem cache duplicados) e zero consumidores internos (`grep -rn SqlBuilder` = 0). Migração: `SqlBuilder.Select(...).From(...).If(...).Build(obj)` → `SqlTemplateLoader.For<Repo>("Sql.Query").Render(obj)` com `.sql` como `EmbeddedResource` (`{IF}{WHERE}{IN}`) ou `SqlTemplate.Parse(sql).Render(obj)` para ad-hoc. Ver `MIGRATION.md` §1. **(US-SQL-000)**
- **Data — `ColumnAttribute` remoção planejada (Sprint 02)** — `src/Footing.Framework/Data/ColumnAttribute.cs` **permanece em `0.2.0-alpha`**; será removido em `0.3.0-alpha` (US-CLEAN-001). Atributo órfão sem leitura em `src/`; Dapper já mapeia via `DefaultTypeMap.MatchNamesWithUnderscores = true`, `CustomPropertyTypeMap` ou `AS` no SQL. Migração em `MIGRATION.md` §2. **(US-CLEAN-001 — planejado)**
- **Package metadata — `Footing.Framework.csproj`**: `Description` sem `SqlBuilder +` e `PackageTags` sem `sqlbuilder`. `README.md` e `src/Footing.Framework/README.md` seção `### SqlBuilder (Fluent API)` removida. **(US-SQL-000)**

### Fixed

- **Data — `SqlTemplate` `IF` com `null` não deve renderizar (B4)** — `RenderIfBlocks()` agora retorna `""` quando `val == null` ou param ausente ou `string.Empty`; antes `null` renderizava `AND ...` indevidamente. `{IF:Name}` com `new { Name = (string)null }` não renderiza mais. **(US-SQL-001)**
- **Data — `SqlTemplate` `IN`/`IFNOTEMPTY` com `string` como `IEnumerable<char>` (B6)** — `RenderInBlocks()` e `RenderIfNotEmptyBlocks()` checam `val is string` **antes** de `IEnumerable`; `string` nunca é tratada como coleção. `Roles = "admin"` com `{IN:Roles}` não renderiza; exige `string[]`/`List<string>`. **(US-SQL-001)**
- **Data — `SqlTemplate` `IFNOTNULL` distingue `null` de `""`** — `RenderIfNotNullBlocks()` renderiza com `""` (empty ≠ null) e não renderiza com `null`/ausente, conforme `RN` de `IFNOTNULL`. **(US-SQL-001)**
- **Data — `SqlTemplate` `IN` com coleção de `null`s / vazia** — `RenderInBlocks()` exige ≥1 item não-nulo; `new int[0]` e `new List<object>{ null, null }` não renderizam; enumerator descartado com `using`. **(US-SQL-001)**
- **Data — `SqlTemplate` `WHERE` normalização** — `NormalizeWhereContent()` remove `AND`/`OR` líder e `RenderWhereBlocks()` omite `WHERE` quando todo conteúdo interno falhou. **(US-SQL-001)**
- **Data — `SqlTemplate` `ToDictionary` com cache — entregue em `0.3.0` mas já em branch** — mantido para histórico 0.2.0 snapshot.
- **EventBus — `Channel` bounded com backpressure (B1)** — `ConcurrentQueue` + `Task.Delay(10)` trocado por `Channel<object>` bounded (`BoundedChannelOptions(capacity 1000, FullMode.Wait, SingleReader false, SingleWriter false)`); `WorkerLoop` usa `await reader.ReadAllAsync(ct)` sem `Delay`; `Publish` via `TryWrite` com warn se cheio, `PublishAsync` via `WriteAsync(ct)` com backpressure; `StopAsync` drena via `Writer.TryComplete()` + `WhenAll(workers)`; `EventBusOptions` exposto via `AddEventBus(o=>...)` com `Capacity`, `FullMode`, `DrainOnStop`. **(US-EVB-001)**
- **Lifecycle — `PreDestroy` async sem deadlock (B2)** — `LifecycleProxy.DisposeCore` agora usa `Task.Run(async()=>await task).GetAwaiter().GetResult()` em vez de `GetAwaiter().GetResult()` direto; `DisposeAsyncCore` usa `await ConfigureAwait(false)`; `PostConstruct` async também via `Task.Run`; `ResolverExtensions.FindPreDestroy` valida múltiplos métodos e lança `InvalidOperationException`. **(US-EVB-002)**
- **DI — `RegisterEventListeners` idempotente + `DependencyWalk` seguro (B3)** — `ResolverExtensions` dedup via `ConcurrentDictionary<(Type,MethodInfo),bool>` e `EventBus` dedup interno; `DependencyWalkExtension` filtra por `GetName().Name` em vez de `FullName.StartsWith`; `AddDependencyWalk` com `try/catch ReflectionTypeLoadException`. **(US-EVB-002)**
- **DI — `DependencyWalkExtension` scanning seguro (parcial em `0.2.0-alpha`)** — `AddDependencyWalk` já filtra por `Assembly.GetName().Name` e trata `ReflectionTypeLoadException` — **(US-EVB-002)**.

### Deprecated

- **`TypeHandlerRegistry` (`Data/TypeHandler.cs:29-50`)** — Marcado `[Obsolete("Use SqlMapper.AddTypeHandler (Dapper) diretamente. TypeHandlerRegistry será removido em 1.0.")]`. Registry global é wrapper fino sobre Dapper; prefira `SqlMapper.AddTypeHandler(new MeuHandler())` / `SqlMapper.AddTypeHandler(typeof(T), handler)`. `ITypeHandler`/`ITypeHandler<T>` e handlers concretos (`JsonTypeHandler`, `EnumTypeHandler`, `BoolCharTypeHandler`, `StringEnumTypeHandler`) **permanecem**. **(US-CLEAN-001)**
- **`DependencyWalkExtension.AddDependencyWalkAuto(rootNamespace)` (`DI/DependencyWalkExtension.cs:10-17`)** — Marcado `[Obsolete("Use AddDependencyWalk(rootNamespace, config, assemblies) com assemblies explícitos. AddDependencyWalkAuto será removido em 1.0.")]`. Motivo: scan por `FullName` e `AppDomain.CurrentDomain.GetAssemblies()` é não-determinístico. **(US-DI-001)**

### Changed

- Documentação `README.md` reestruturada para documentar apenas `SqlTemplate`/`SqlTemplateLoader` (tags `{IF}{IFNOTNULL}{IFNOTEMPTY}{IN}{WHERE}{CHOOSE}`) como API oficial de SQL dinâmico. **(US-SQL-000)**
- Documentação `README.md` EventBus atualizada para `Channel` bounded (`Capacity`, `FullMode`, `DrainOnStop`, `Publish`/`PublishAsync` com backpressure) e sem `Delay(10)`. **(US-EVB-001)**
- `SqlResult` mantido sem alterações — contrato `Sql` + `Parameters` compatível entre `SqlBuilder.Build` e `SqlTemplate.Render`.

### Added

- `MIGRATION.md` — guia completo `0.1.0-alpha → 0.2.0-alpha` com comparativo antes/depois (`SqlBuilder` vs `SqlTemplateLoader.For<Repo>` + `.sql` `EmbeddedResource` + `{IF}{WHERE}{IN}`), passos `< 15 min` por query e notas sobre `ColumnAttribute`/`TypeHandlerRegistry`/`AddDependencyWalkAuto`. **(US-SQL-000)**

---

## [0.1.0-alpha.1] - 2026-09-04

### Added

- Microframework base (.NET 10) — EventBus in-process com handlers tipados/priorizados (`EventBus`, `IEventHandler<T>`, `[EventListener]`), Data Layer (`SqlBuilder` + `SqlTemplate`/`SqlTemplateLoader` + `IDbConnectionFactory` + `UnitOfWork` + `TypeHandler` + `ColumnAttribute`), DI auto-scan (`AddDependencyWalk`/`AddDependencyWalkAuto`, `[Injectable]`/`[Service]`/`[Repository]`), Lifecycle (`[PostConstruct]`/`[PreDestroy]`), Config (`[InjectConfig]`).
- `Footing.Framework.csproj` `0.1.0-alpha.1` — `PackageId Footing.Framework`, `IsPackable true`, `GenerateDocumentationFile true`.
- `README.md` e `src/Footing.Framework/README.md` com documentação completa de EventBus, Data Layer, DI, Lifecycle e Config.

[3.4.2]: https://github.com/etelhado/Footing.Framework/compare/v3.3.0...v3.4.2
[3.3.0]: https://github.com/etelhado/Footing.Framework/compare/v3.2.0...v3.3.0
[3.2.0]: https://github.com/etelhado/Footing.Framework/compare/v3.1.0...v3.2.0
[3.1.0]: https://github.com/etelhado/Footing.Framework/compare/v3.0.0...v3.1.0
[3.0.0]: https://github.com/etelhado/Footing.Framework/compare/v2.4.0...v3.0.0
[2.4.0]: https://github.com/etelhado/Footing.Framework/compare/v2.3.1...v2.4.0
[2.3.1]: https://github.com/etelhado/Footing.Framework/compare/v2.3.0...v2.3.1
[2.3.0]: https://github.com/etelhado/Footing.Framework/compare/v2.2.0...v2.3.0
[2.3.0-alpha]: https://github.com/etelhado/Footing.Framework/compare/v2.2.0...v2.3.0-alpha
[2.2.0]: https://github.com/etelhado/Footing.Framework/compare/v2.1.0...v2.2.0
[2.2.0-alpha]: https://github.com/etelhado/Footing.Framework/compare/v2.1.0...v2.2.0-alpha
[2.1.0]: https://github.com/etelhado/Footing.Framework/compare/v2.0.0...v2.1.0
[2.0.0]: https://github.com/etelhado/Footing.Framework/compare/v2.0.0-alpha.1...v2.0.0
[2.0.0-alpha.1]: https://github.com/etelhado/Footing.Framework/compare/v2.0.0-alpha...v2.0.0-alpha.1
[2.0.0-alpha]: https://github.com/etelhado/Footing.Framework/compare/v1.0.0...v2.0.0-alpha
[1.1.0-alpha]: https://github.com/etelhado/Footing.Framework/compare/v1.0.0...v1.1.0-alpha
[1.0.0]: https://github.com/etelhado/Footing.Framework/compare/v0.3.0-alpha...v1.0.0
[0.3.0-alpha]: https://github.com/etelhado/Footing.Framework/compare/v0.2.0-alpha...v0.3.0-alpha
[0.2.0-alpha]: https://github.com/etelhado/Footing.Framework/compare/v0.1.0-alpha.1...v0.2.0-alpha
[0.1.0-alpha.1]: https://github.com/etelhado/Footing.Framework/releases/tag/v0.1.0-alpha.1
