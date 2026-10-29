<p align="center"><img src="docs/logo.png" width="96" alt="Bewerbo logo"></p>

# Bewerbo

A small Windows app for keeping track of job and Ausbildung applications.

I built Bewerbo for my own applications in Germany: one place to see where I applied, what the status is,
when to follow up, which documents I sent, and how many applications are still open for each position. Everything stays on your
computer in a single JSON file. There is no account and no server.

## Features

- **Applications at a glance.** Each card shows the company, position, type, salary, dates, notes and
  the company's website icon. Change the status straight from the card.
- **Your own types.** Give each application any type you like, such as Job, Ausbildung, Internship or
  Werkstudent. The sidebar lists every type with a count.
- **Overview.** Open applications per type, plus interviews, offers, follow-ups that are due, and rejections.
- **Search, filter and sort** by type, status, position, company, notes or location. Positions are grouped
  with a count for each, so you can see how many went out for every role.
- **Documents.** Point Bewerbo at the folder where you keep one subfolder per company (for example
  `Documents\Applications\Siemens`). Add your cover letter and CV for each application right in the form, or drop
  files on its card. Each card shows which documents of your checklist are in the folder (cover letter, CV and
  certificates by default; add your own in Settings) and opens the folder with one click. New folders start with
  a copy of your standard documents, minus the ones you brought yourself. **Import from folders** turns company
  folders you already have into applications. Bewerbo only creates folders and copies files in; it never deletes,
  moves or overwrites anything.
- **Paste details.** Copy a block like the one below and press `Ctrl+V` in the main window, or use
  **Paste details** in the form, and Bewerbo fills everything in. When the form opens with such a block on the
  clipboard, it offers to fill it in. **Copy prompt** in the form copies a short prompt for a chat assistant so
  it always answers in this format. Markdown bullets, bold labels and code fences are fine.

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
- **Light and dark themes.** Follows the Windows setting by default, or pick one in Settings.
- **Keyboard:** `Ctrl+N` adds an application, `Ctrl+V` adds one from copied details, `Ctrl+F` jumps to search,
  `Esc` clears it, `Ctrl+,` opens Settings.

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
  Bewerbo.Core/     Domain model, storage, settings, documents, filtering, summary and the paste parser. No UI code.
  Bewerbo/          WPF app: views, view models, theme and Windows services.
tests/
  Bewerbo.Core.Tests/   Unit tests for the core library.
  Bewerbo.Tests/        UI smoke test that opens the real windows and walks through the main flows.
```

The app uses plain MVVM without a framework: view models expose bindable properties and commands,
and anything that needs a window (dialogs, file pickers, opening links) goes through `IDialogService`.

## Data

Applications are stored in `%LOCALAPPDATA%\Bewerbo\applications.json`, with the previous save in
`applications.json.bak`, settings in `settings.json` and cached website icons in `icons\`. Your documents stay
in the folders you chose; the data file only stores the path of each application's folder. Website icons are fetched from
Google's favicon service using only the host name of the job link.

## License

[MIT](LICENSE)
