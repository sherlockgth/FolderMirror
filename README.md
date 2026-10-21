# FolderMirror

A silent background folder mirroring tool for Windows.

## What it does

- Watches a source folder and mirrors it to a target folder
- Skips syncing when the target folder is unavailable (for example E: drive is not mounted or cloud drive is disconnected)
- Keeps running in the background silently
- Supports changing the source and target paths in `appsettings.json`

## Build

```bash
dotnet restore
dotnet build -c Release
```

Publish single-file exe:

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## Run

```bash
FolderMirror.exe
```

The app reads `appsettings.json` from the same directory or from the current working directory.

## Configure

Edit `appsettings.json`:

```json
{
  "SourceFolder": "C:\\YourSourceFolder",
  "TargetFolder": "E:\\YourTargetFolder",
  "PollIntervalSeconds": 5,
  "DeleteExtraFiles": true,
  "LogPath": "C:\\Tools\\FolderMirror\\foldermirror.log"
}
```
