# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

A .NET class library that wraps sports-reference data sources. The first client is
`Teqniqly.BaseballReferenceClient`; additional sport clients follow the same shape.

## Layout

- `Teqniqly.SportsReferenceClient.slnx` — solution (XML `.slnx` format; needs a recent SDK).
- `BaseballReferenceClient/Teqniqly.BaseballReferenceClient/` — the library.
- `BaseballReferenceClient/Teqniqly.BaseballReferenceClient.Tests/` — xUnit v3 test project.
- `.githooks/` — version-controlled git hooks (see below).
- `.github/workflows/ci.yml` — CI (format check + build/test + SonarCloud).

## Build & tooling

- **Target framework:** `net10.0` only. Requires the **.NET 10 SDK**; the version is pinned in
  `global.json` (`rollForward: latestFeature`) and drives both local builds and CI.
- **Central Package Management:** all versions live in `Directory.Packages.props`. Add a
  `PackageVersion` there, then reference the package without a version in the `.csproj`.
- **`nuget.config`:** single source (`nuget.org`) with package source mapping — do not add a
  second feed without a matching `packageSourceMapping` entry or restore fails.
- **`Directory.Build.props`:** nullable + implicit usings on, `TreatWarningsAsErrors=true`,
  SonarAnalyzer.CSharp, and test packages injected into any project whose name ends in `Tests`.
- Common commands:
  - `dotnet build` / `dotnet test`
  - `dotnet csharpier format .` (format) · `dotnet csharpier check .` (verify; CI gate)
  - `dotnet tool restore` (restores CSharpier from `dotnet-tools.json` at the repo root)

## Conventions

- **Formatting is owned by CSharpier** (`dotnet-tools.json`, v1.x — use the `format`/`check`
  subcommands). Do not hand-format; run CSharpier. The pre-commit hook enforces it.
- **Commit messages: Conventional Commits** (`<type>(<scope>)!: <desc>`, where scope and `!`
  are optional), enforced by the `commit-msg` hook. Types:
  `feat fix docs style refactor perf test build ci chore revert`.
- **U.S. English** spelling in code, comments, and docs.
- **Line endings:** LF everywhere except `.sln/.slnx/.ps1/.bat/.cmd` (CRLF). Governed by
  `.gitattributes` and mirrored in `.editorconfig`; keep the two aligned.

## Git hooks

Hooks live in `.githooks/` and are activated by `core.hooksPath`. The `ConfigureGitHooks`
target in `Directory.Build.props` sets this idempotently on every `dotnet build`; a fresh clone
can also run `git config core.hooksPath .githooks` manually.

- `pre-commit` — runs CSharpier on staged `.cs/.csproj/.props/.targets/.config/.slnx` files
  (including renames) and re-stages them.
- `commit-msg` — validates the subject against Conventional Commits.

## CI & branch protection

`.github/workflows/ci.yml` runs on push/PR to `main` and manual dispatch:

- **Format Check** (job id `format-check`) — `dotnet csharpier check .` (fast gate).
- **Build, Test and SonarCloud Analysis** (job id `sonarcloud-analysis`) — build + test with
  coverage + SonarCloud analysis (quality-gate wait).

`main` is protected: both checks are **required** and the branch must be up to date before
merge. If you rename a job's `name:`, update the required-check contexts in branch protection
in the same change, or PRs will block. Actions are SHA-pinned — keep the `# v4` comments when
bumping.

> Fork PRs cannot access `SONAR_TOKEN`, so the SonarCloud job fails for external contributors.
> This is accepted (the repo does not expect forks); do not "fix" it by weakening the trigger.
