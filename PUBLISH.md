# Publish — Footing.Framework

## Estrutura criada

```
 /home/etelhado/Pessoais/Footing.Framework/
├── src/Footing.Framework/Footing.Framework.csproj  # IsPackable, PackageId=Footing.Framework, Version=3.4.2
├── Footing.Framework.sln
├── README.md        # PackageReadmeFile
├── LICENSE (MIT)
├── icon.png         # PackageIcon 128x128 (3.4.2)
├── nuget.config
└── .github/workflows/publish.yml  # pack + push em tags v*
```

`Academia/` agora referencia via **ProjectReference relativo**:
`../../Footing.Framework/src/Footing.Framework/Footing.Framework.csproj` (Academia.Core/Web/Worker + Academia.slnx).
O diretório legado `Academia/Footing.Framework/` permanece como *deprecated* (`README_DEPRECATED.md`) até remoção.

## Comandos locais

```bash
# build
dotnet build Footing.Framework.sln -c Release

# pack (gera .nupkg + .snupkg) — 3.5.1 477t 11 regex 78K
dotnet pack src/Footing.Framework/Footing.Framework.csproj -c Release -o ./artifacts

# inspecionar
ls -lh artifacts/
dotnet nuget verify artifacts/Footing.Framework.*.nupkg

# teste local via feed local (sem publicar)
dotnet pack -c Release -o /tmp/local-feed
dotnet nuget add source /tmp/local-feed --name local-footing  # uma vez
# em Academia, trocar temporariamente para PackageReference para teste:
# <PackageReference Include="Footing.Framework" Version="3.4.2" /> (stable 3.4.2, 473 testes, ~78K, 11 regex, 3188 LOC <3.5K, APROVADO; anterior 3.3.0 / 3.2.0 / 3.1.0)
```

## Publicar no nuget.org

1. Crie API key em https://www.nuget.org/account/apikeys (push)
2. Configure secret `NUGET_API_KEY` no GitHub repo
3. Crie tag e push (exemplo `v3.5.1` patch):
    ```bash
    git remote add origin https://github.com/etelhado/Footing.Framework  # se `git remote -v` vazio
    git tag v3.5.1
    git push origin v3.5.1
    ```
     O workflow `.github/workflows/publish.yml` executa `dotnet pack` + `dotnet nuget push` em tags `v*`.

Ou manual:
```bash
dotnet pack src/Footing.Framework/Footing.Framework.csproj -c Release -o ./artifacts
dotnet nuget push ./artifacts/Footing.Framework.3.5.1.nupkg \
  --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Versionamento

- `3.5.1` patch atual **Fixed i18n 0.15 + Benchmark formal 0.20 + and revalidação 0 SP (0.35 SP sem or)** sobre `aa37af8` `3.5.0` 477t 2897 LOC 11 regex 78K — `3.5.0 → 3.5.1` patch SemVer `Fixed` + `perf Could`, 0 breaking, 0 LOC prod, `or` Won't definitivo 0% 0/208 t-odonto vs `and` 34.1% 71/208 Must DONE, `GLOBAL_RULES 60L`. `3.5.0` anterior portal Antora 0.3 + UseTransaction Won't + i18n 0.15 477t; `3.4.2` 473t 3188 LOC; `3.3.0` Migrations Y/N 371t. Tag `v3.5.1` pendente — só após auditoria @aud_carlos APROVADO ≥9.0 (workflow `publish.yml` dispara em `v*`, não taguear nesta sprint).
- Bumps: edite `<Version>` no csproj. Em CI com tag `vX.Y.Z`, a versão da tag deve casar. `csproj:Version` atual `3.5.1` **patch 0.35 SP sem or** (SemVer §8.15).
- Stable: `1.0.0` foi o primeiro SemVer estável; `2.0.0` segundo stable; `2.1.0` terceiro minor additive; `2.2.0` quarto stable promoção; `2.3.0` quinto stable fechamento (Must/Should); `2.3.1` patch docs (Must Have 0.5 SP); `2.4.0` minor (Must 1 + Should 1.5 =2.75 SP); `3.0.0` major breaking (Must Have 1.5 SP); `3.1.0` minor Must 4.5 SP + veto-final 1.6 SP; `3.2.0` minor sem-migrations 1.5 SP; `3.3.0` minor Migrations Y/N 0.7 SP; `3.4.0` 0.7 SP resiliência 389t; `3.4.1` 0.5 SP 4 DBs 393t; `3.4.2` patch stable promoção 0.2 SP 473t; `3.5.0` portal Antora 0.3 + UseTransaction Won't + i18n 0.15 477t 2897 LOC; `3.5.1` patch Fixed i18n + Benchmark formal 0.35 SP sem or 477t. Notas em `MIGRATION.md: 8.2 → 8.3 → 8.5 → 8.6 → 8.7 → 8.8 → 8.9 → 8.10 → 8.11 → 8.12 → 8.13 → 8.14 → 8.15`. `0.1.0-alpha → 3.5.1` histórico completo em `CHANGELOG.md`.

## Migrar Academia para PackageReference (quando publicado)

Em `Academia.Core/Academia.Core.csproj`, `Academia.Web/`, `Academia.Worker/`:

```xml
<!-- remover -->
<ProjectReference Include="..\..\Footing.Framework\src\Footing.Framework\Footing.Framework.csproj" />
<!-- adicionar -->
<PackageReference Include="Footing.Framework" Version="3.5.1" />
```

E em `Academia.slnx`, remover a linha do Footing externo. Depois deletar `Academia/Footing.Framework/` legado.

## Próximos passos sugeridos (Sprint 3.4 DONE → 3.5)

- **3.4.2 stable 0.2 SP DONE promoção docs/tag sem código (Must 0.2 SP sobre d61ef60):** `Version 3.4.2` + `CHANGELOG [3.4.2] stable sem breaking` + `MIGRATION §8.13` + `PUBLISH 3.4.2` + `README 11 tags verificado` + `print-38.md` roadmap `3.4.2→3.5.0→4.0.0` — 371→473t +102, 11→11 regex, 3188 LOC, pack 78K, `Migrations/` Y/N + resiliência + 4 DBs + Gilson 80 agnóstico 100%. Tag `v3.4.2` pendente auditoria @aud_carlos (não taguear nesta sprint).
- **3.5 Should futuro 1.0 SP → 3.5.0 revisado Won't definitivo:** `UseTransaction` **Won't definitivo 0 SP** (DDL transacional agnóstico não suportado, mesmo com BUY — `MSSQL` tolera mas não recomenda, `Firebird/dbf/SQLite` não permitem DDL em transação; `AutoTransaction=false` agnóstico, `BEGIN/COMMIT` no `.sql` se PG; `throw NotSupportedException` descritivo) + docs Antora Must 0.3 + perf Could 0.2 — ver `MIGRATION §8.14` roadmap `3.4.2→3.5.0→4.0.0` **Won't, sem BUY**.
- **3.3.0 Migrations Y/N 0.7 SP anterior:** `Version 3.3.0` + `CHANGELOG [3.3.0] Migrations Y/N` + `MIGRATION §8.12` — 371t, 68K → `3.4.2` stable promoção 473t.
- `git remote` já configurado: `git remote -v` deve mostrar `origin https://github.com/etelhado/Footing.Framework (fetch/push)` — ver `MIGRATION §8.13`.
