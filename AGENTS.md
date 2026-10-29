# Notes for contributors and coding agents

Bewerbo is a single-user WPF desktop app (.NET 10, Windows) that tracks job and Ausbildung applications.
This file explains how the code is organised and which rules must not be broken.

## Build and test

```powershell
dotnet build Bewerbo.slnx
dotnet test Bewerbo.slnx
dotnet publish src/Bewerbo -p:PublishProfile=win-x64   # single-file exe in .\publish
```

- `TreatWarningsAsErrors` is on for every project (see `Directory.Build.props`). Keep the build warning-free.
- `Bewerbo.Core` and `Bewerbo.Core.Tests` run on any OS. `Bewerbo` and `Bewerbo.Tests` need Windows to run,
  but they build on Linux thanks to `EnableWindowsTargeting`.
- CI (`.github/workflows/ci.yml`) builds and tests on `windows-latest` and uploads the screenshots taken by
  the UI test as an artifact. Pushing a `v*` tag runs `release.yml`, which publishes a zip to GitHub Releases.

## Layout

| Path | What lives there |
| --- | --- |
| `src/Bewerbo.Core/JobApplication.cs` | The model. Dates are `DateOnly?`, timestamps are UTC `DateTime`. |
| `src/Bewerbo.Core/ApplicationDraft.cs` | Editor form state: validation and building a `JobApplication`. |
| `src/Bewerbo.Core/ApplicationQuery.cs` | `ApplicationFilter` and sorting. |
| `src/Bewerbo.Core/Positions.cs` | Grouping by position (case and whitespace insensitive). |
| `src/Bewerbo.Core/ApplicationSummary.cs` | Sidebar overview numbers. |
| `src/Bewerbo.Core/DetailsParser.cs` | "Field: value" parser behind the Paste details button. |
| `src/Bewerbo.Core/Storage/` | JSON format, atomic save with backup, import merge, data folder. |
| `src/Bewerbo/ViewModels/` | `MainViewModel`, `EditorViewModel`, `ApplicationCardViewModel`, small MVVM helpers. |
| `src/Bewerbo/Views/` | `MainWindow` and `EditorWindow`. Code-behind only for focus, keyboard and clipboard. |
| `src/Bewerbo/Services/` | Dialogs, favicon cache, dark title bar. |
| `src/Bewerbo/Themes/` | `Colors.xaml` (all colours) and `Controls.xaml` (all styles). |

Business rules belong in `Bewerbo.Core` with a unit test. View models only translate between the core and the
views. Do not add NuGet packages to the app or the core library without a strong reason.

## Data format (do not break)

User data lives in `%LOCALAPPDATA%\Bewerbo\applications.json`. Real data is in there, so every change must keep
existing files readable.

- The file is `{ "Version": 1, "Applications": [ ... ] }` with PascalCase property names.
- `Type` and `Status` are stored as enum names (`"Ausbildung"`, `"Applied"`, ...). Renaming an enum member
  breaks existing files; add new members at the end only.
- `AppliedOn` and `FollowUpOn` are `"YYYY-MM-DD"` strings and are written as `""` when empty.
- `Created` and `Updated` are round-trip (`"o"`) UTC timestamps.
- Files written by the original Apply app (the project's earlier name) must keep loading. `tests/Bewerbo.Core.Tests/Fixtures/version1-backup.json`
  is such a file and is covered by a test.
- On first start, `DataFolder.CopyLegacyData` copies data from the old `%LOCALAPPDATA%\ApplyTracker` folder.
  It never deletes or changes the old folder.
- If the format ever has to change, bump `ApplicationStore.FormatVersion` and add a migration with tests.

## Behaviour worth knowing

- "Remaining" in the overview means every application that is not Rejected or Withdrawn.
- A follow-up is due when its date is today or earlier and the status is not Rejected, Withdrawn or Accepted.
- New applications start as Applied with today's date. Setting a status to Applied fills in today's date
  when no application date is set.
- Links without a scheme get `https://` added. Card icons come from
  `https://www.google.com/s2/favicons?sz=64&domain=<host>` and are cached in `icons\<host>.png`.
  When no icon can be loaded, the card shows the company initials.
- Only one instance can run at a time (named mutex `Local\Bewerbo`).

## Style

- File-scoped namespaces, nullable reference types, `var` where the type is obvious, `_camelCase` private fields.
- Keep colours in `Themes/Colors.xaml` and reuse the named brushes. The palette is a neutral near-black dark
  theme: window `#191919`, sidebar `#1F1F1F`, cards `#222222`, controls `#303030`; blue for actions, green,
  yellow and red for statuses.
- Comments explain why, not what. Keep them rare.
