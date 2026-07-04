# `sportsref` CLI

A [Spectre.Console](https://spectreconsole.net) console app that downloads sports-reference data from the terminal. The build output binary is named `sportsref`.

## Download a baseball schedule

Fetch a season's schedule page and save it to a file:

```sh
sportsref baseball schedule get --year 2026 --file c:\temp\baseballref-schedule-2026.shtml
```

Both options are required:

| Option | Description |
| --- | --- |
| `--year` | Season year, `1871` through the current UTC year. |
| `--file` | Output path for the raw `.shtml` file. Missing directories are created. |

The response is written verbatim (the raw `.shtml` page -- no parsing). On success the command prints the byte count and exits `0`; an invalid year or file, a network/HTTP failure, or cancellation (Ctrl+C) exits non-zero.

## Running

During development, run through the SDK from the repo root:

```sh
dotnet run --project Utilities/CLI/Teqniqly.SportsReferenceClient.Cli -- baseball schedule get --year 2026 --file schedule.shtml
```

`--help` is available at every level:

```sh
sportsref --help
sportsref baseball schedule get --help
```

## Configuration

The base address is read from [`appsettings.json`](appsettings.json) (shipped next to the binary) and can be overridden with an environment variable:

```sh
# override the Baseball Reference schedule base address
set BaseAddresses__BaseballReference__ScheduleClient=https://www.baseball-reference.com/leagues/majors/
```
