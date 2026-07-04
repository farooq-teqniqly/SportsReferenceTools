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
- **Commit messages: Conventional Commits** (`<type>(<scope>)!: <description>`, where scope and
  `!` are optional and a scope must match `[a-z0-9._-]+`), enforced by the `commit-msg` hook.
  Types: `feat fix docs style refactor perf test build ci chore revert`.
- **U.S. English** spelling and grammar in all code, comments, and docs (e.g. "canceled" not
  "cancelled", "behavior" not "behaviour", "normalize" not "normalise"). Do not "correct" .NET
  API names that use other spellings (e.g. `CancellationToken`) -- match identifiers exactly.
- **Default accessibility:** classes are `internal sealed` unless a `public` surface is
  genuinely required (e.g. a library entry point, or a type a framework must discover such as
  xUnit test classes). Abstract/base classes stay `internal` (they can't be `sealed`).
  **Interfaces are `public` by default.** When a type must be `internal` but a trusted assembly
  needs it (test project, Castle/NSubstitute proxy `DynamicProxyGenAssembly2`), grant access via
  `<InternalsVisibleTo>` rather than widening the type to `public`.
- **TDD for production code:** follow red-green-refactor. Write a failing test first and run it
  to confirm it fails for the right reason (**red**); write the minimum production code to make
  it pass (**green**); then clean up with the tests still passing (**refactor**). Do not add or
  change production behavior without a test that first failed. (Non-behavioral edits -- docs,
  formatting, accessibility/signature tweaks like adding a `= default` -- do not need a new test.)
- **No primary constructors.** Use explicit constructors with backing fields (`_field`), not
  `class Foo(...)` primary constructors, for classes and structs. `IDE0290` (the "use primary
  constructor" hint) is set to `none` in `.editorconfig`; no analyzer errors on usage, so this
  is enforced by convention and review.
- **Null-guard public/internal entry points.** Every reference-type parameter of a public or
  internal method (including `this` on extension methods) is guarded before use:
  `ArgumentNullException.ThrowIfNull(x)` for objects, `ArgumentException.ThrowIfNullOrWhiteSpace(s)`
  for required strings. Guards come first, in parameter order, before any other work. Document
  each with an `<exception>` tag. Do not rely on a downstream call to throw for you.
- **`CancellationToken` parameters** take a default (`CancellationToken cancellationToken = default`)
  and are the last parameter, so callers may omit them.
- **XML documentation** is required on every `public` and `internal` type and member in library
  (non-test) projects: at minimum a `<summary>`, plus `<param>`, `<returns>`, and `<exception>`
  where they add information. Use an explicit `<inheritdoc />` on every member that overrides or
  implements a documented base-type/interface member (interface implementations, `override`
  methods and properties, e.g. `Stream` overrides) instead of copying text or relying on implicit
  inheritance; add `<inheritdoc />` plus an extra `<exception>`/`<remarks>` tag when the member
  adds behavior the base does not document. Library projects set
  `<GenerateDocumentationFile>true</GenerateDocumentationFile>`,
  so missing docs on `public` members fail the build (`CS1591` under `TreatWarningsAsErrors`);
  `internal` members are covered by convention, not the compiler. Test projects are exempt —
  test names document intent.
- **No superfluous comments.** Do not add comments that restate what the code already says
  (types, names, obvious control flow). Comment only the non-obvious: *why* a choice was made,
  a workaround, an analyzer/framework quirk, or a subtle invariant. When in doubt, leave it out.
- **Line endings:** LF everywhere except `.sln/.slnx/.ps1/.bat/.cmd` (CRLF). Governed by
  `.gitattributes` and mirrored in `.editorconfig`; keep the two aligned.

## Testing

xUnit v3 + NSubstitute. `Directory.Build.props` sets `TreatWarningsAsErrors=true` but
`CodeAnalysisTreatWarningsAsErrors=false`, so `CA*` analyzer findings are warnings (they do not
fail the build) while plain compiler warnings do. Keep tests warning-clean anyway. Recurring
analyzer gotchas:

- **CA2000 (dispose `IDisposable` before it goes out of scope).** Any `HttpClient`,
  `HttpResponseMessage`, `HttpContent`, etc. created in a test must be owned and disposed —
  do not `new` one and drop it. Pattern in `ScheduleClientTests`: the test class implements
  `IDisposable`, factory/helper methods add each created disposable to a `List<IDisposable>`
  field, and `Dispose()` disposes them all (xUnit makes a fresh test-class instance per test).
  Disposing an `HttpClient` also disposes the `HttpMessageHandler` it was constructed with.
- **CA2007 (`ConfigureAwait`)** is disabled for `*Tests.cs` in `.editorconfig` — do **not** add
  `ConfigureAwait` in tests. xUnit's `xUnit1030` forbids `ConfigureAwait(false)` in test
  methods, and `ConfigureAwait(true)` trips SonarAnalyzer `S125` (reads as commented-out code).
- **xUnit1051 (`CancellationToken`)** fires when a token-accepting call *omits* the token and
  relies on the default; passing `CancellationToken.None` explicitly satisfies it. Do that in
  tests rather than omitting the argument.
- **Shared test double:** `TestHttpMessageHandler` lives in `Teqniqly.SportsReferenceClient.Common.Tests`
  (internal, exposed to `DynamicProxyGenAssembly2` and to `Teqniqly.BaseballReferenceClient.Tests`
  via `InternalsVisibleTo`). Other test projects reuse it through a project reference — do not
  duplicate it.
- **Faking `HttpClient`:** `HttpMessageHandler.SendAsync` is `protected`, so NSubstitute can't
  intercept it. Use the `TestHttpMessageHandler` wrapper (public abstract `MockSendAsync` +
  `sealed` protected `SendAsync` override that delegates to it) so only `MockSendAsync` is
  substituted.
- **Internal types under test** (e.g. `ScheduleClient`) are reached via `<InternalsVisibleTo>`
  in the library `.csproj`, not by widening their accessibility.

## Git hooks

Hooks live in `.githooks/` and are activated by `core.hooksPath`. The `ConfigureGitHooks`
target in `Directory.Build.props` attempts to set this on every `dotnet build`, but it is
best-effort: it only runs when a `.git` directory exists and swallows failures
(`ContinueOnError="true"`). If hooks aren't firing, run `git config core.hooksPath .githooks`
manually.

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
