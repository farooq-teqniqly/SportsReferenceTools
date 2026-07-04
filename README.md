# Teqniqly.SportsReferenceClient

## Prerequisites

- **.NET 10 SDK** (`10.0.300` or later). All projects target `net10.0`. The SDK is pinned in [`global.json`](global.json) (`rollForward: latestFeature`), so a clean environment fails fast with a clear message if a compatible SDK is missing. Install from <https://dotnet.microsoft.com/download/dotnet/10.0>.

## Command-line interface

`sportsref` is a [Spectre.Console](https://spectreconsole.net) console app for downloading sports-reference data from the terminal (e.g. `sportsref baseball schedule get --year 2026 --file schedule.shtml`). See [`Utilities/CLI/Teqniqly.SportsReferenceClient.Cli/README.md`](Utilities/CLI/Teqniqly.SportsReferenceClient.Cli/README.md) for usage.

## Developer setup

### Git hooks

This repo ships version-controlled git hooks in [`.githooks/`](.githooks):

- **`pre-commit`** — formats staged C# files with [CSharpier](https://csharpier.com) (pinned in [`dotnet-tools.json`](dotnet-tools.json)) and re-stages them.
- **`commit-msg`** — enforces [Conventional Commits](https://www.conventionalcommits.org) on the subject line.

Git does not use these hooks until it is pointed at the directory. Run once per clone:

```sh
git config core.hooksPath .githooks
```

This step is automated by an MSBuild target (`ConfigureGitHooks` in `Directory.Build.props`), so the first `dotnet build` also sets it for you. Running it manually just makes the hooks active before your first build.

Restore the local tools (CSharpier) so the `pre-commit` hook can run:

```sh
dotnet tool restore
```

### Commit message format

Commit subjects must follow:

```
<type>(<scope>)!: <description>
```

- **type** (required): `feat` `fix` `docs` `style` `refactor` `perf` `test` `build` `ci` `chore` `revert`
- **scope** (optional): lower-case component name, e.g. `(client)`
- **!** (optional): marks a breaking change
- **description** (required): short summary

Examples:

```
feat(client): add player season stats lookup
fix!: drop legacy field from response model
docs: document rate-limit handling
```

Merge, revert, and `fixup!`/`squash!` commits are exempt.

### Formatting

Format the whole solution manually at any time:

```sh
dotnet csharpier format .
```

Check formatting without writing changes (useful in CI):

```sh
dotnet csharpier check .
```
