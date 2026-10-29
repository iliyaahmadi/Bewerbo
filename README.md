# Bewerbo

A small Windows app for keeping track of job and Ausbildung applications.

I built Bewerbo for my own applications in Germany: one place to see where I applied, what the status is,
when to follow up, and how many applications are still open for each position. Everything stays on your
computer in a single JSON file. There is no account and no server.

## Features

- **Applications at a glance.** Each card shows the company, position, type, salary, dates, notes and
  the company's website icon. Change the status straight from the card.
- **Overview.** The sidebar counts open applications by Ausbildung and job, plus interviews, offers,
  follow-ups that are due, and rejections.
- **Positions.** Applications are grouped by position with a count for each, so you can see how many
  went out for every role.
- **Search, filter and sort** by type, position, status, company, notes or location.
- **Paste details.** Copy a block like the one below (for example from a chat assistant that helped write the
  cover letter) and Bewerbo fills in the form. Markdown bullets, bold labels and code fences are fine.

  ```
  Company: Example GmbH
  Position: Fachinformatiker für Systemintegration
  Type: Ausbildung
  Status: Applied
  Location: Berlin
  Applied on: 2026-10-25
  Follow up on: 2026-11-08
  Salary: €1,200 / month
  Link: https://example.com/jobs/123
  Notes: Sent CV and cover letter v2.
  ```

- **Safe storage.** Every save replaces the data file atomically and keeps the previous version as a backup.
  Backups can be exported and imported as JSON.
- **Dark theme**, with a title bar that follows the Windows light/dark setting.
- **Keyboard:** `Ctrl+N` adds an application, `Ctrl+F` jumps to search, `Esc` clears it.

## Download

Get `Bewerbo-<version>-win-x64.zip` from [Releases](../../releases), unzip it and run `Bewerbo.exe`.
It is a single self-contained file for 64-bit Windows 10 and 11, so no .NET installation is needed.

## Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build
dotnet test
dotnet run --project src/Bewerbo

# Single-file Bewerbo.exe in .\publish
dotnet publish src/Bewerbo -p:PublishProfile=win-x64
```

The core library and its tests also build and run on Linux and macOS. The app and the UI tests need Windows.

## Project structure

```
src/
  Bewerbo.Core/     Domain model, storage, filtering, summary and the paste parser. No UI code.
  Bewerbo/          WPF app: views, view models, theme and Windows services.
tests/
  Bewerbo.Core.Tests/   Unit tests for the core library.
  Bewerbo.Tests/        UI smoke test that opens the real windows and walks through the main flows.
```

The app uses plain MVVM without a framework: view models expose bindable properties and commands,
and anything that needs a window (dialogs, file pickers, opening links) goes through `IDialogService`.

## Data

Applications are stored in `%LOCALAPPDATA%\Bewerbo\applications.json`, with the previous save in
`applications.json.bak` and cached website icons in `icons\`. Website icons are fetched from
Google's favicon service using only the host name of the job link.

## License

[MIT](LICENSE)
