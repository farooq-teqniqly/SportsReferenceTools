# Teqniqly.SportsReferenceClient

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
