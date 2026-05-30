# Apple Converter

Apple Converter is a modern Windows desktop app for converting Apple media files into legacy-friendly formats that older devices, websites, school systems, office apps, and Windows programs can open easily.

The app is built with C#, .NET 8, WinUI 3, and Windows App SDK. The core conversion logic is separated from the UI so media handling, queue behavior, naming, settings, and backend diagnostics are testable.

## Features

- Drag-and-drop files and folders into a WinUI 3 Fluent desktop interface.
- Batch queue with per-file status, progress, failures, retry, filters, cancel, clear completed, and clear queue.
- HEIC/HEIF/JPG/JPEG/PNG/WEBP/TIFF/BMP image input detection.
- Image output to JPG, JPEG, or PNG.
- MOV/MP4/M4V video input detection.
- MOV/video conversion to MP4 using H.264 video and AAC audio through FFmpeg.
- Video first-frame extraction and frame sequence extraction.
- Live Photo pair detection for matching image and `.MOV` filenames.
- Output folder selection, same-folder output, folder structure preservation, flattening, and collision behavior.
- Settings page for theme, default formats, JPEG quality, PNG compression, metadata handling, timestamps, concurrency, folder scanning, FFmpeg path, and logs.
- Backend status page with copyable diagnostics.
- Self-contained installer packaging with bundled FFmpeg support.
- Local JSON settings and local log files.
- GitHub Actions build and draft release workflows.

## Supported formats

Image inputs:

- `.heic`
- `.heif`
- `.jpg`
- `.jpeg`
- `.png`
- `.webp`
- `.tiff`
- `.tif`
- `.bmp`

Video inputs:

- `.mov`
- `.mp4`
- `.m4v`

Outputs:

- `.jpg`
- `.jpeg`
- `.png`
- `.mp4`

## Screenshots

Screenshots placeholder. Add current UI images before publishing the first public release.

## Build locally

Requirements for development:

- Windows 10 19041+ or Windows 11
- .NET 8 SDK
- Windows App SDK dependencies restored from NuGet

Commands:

```powershell
cd D:\GitHub\AppleConverter
dotnet restore AppleConverter.sln
dotnet build AppleConverter.sln -c Release
dotnet test src\AppleLegacyMediaConverter.Tests\AppleLegacyMediaConverter.Tests.csproj -c Release
```

## Run locally

```powershell
cd D:\GitHub\AppleConverter
dotnet run --project src\AppleLegacyMediaConverter\AppleLegacyMediaConverter.csproj -c Release -p:Platform=x64
```

## Package locally

```powershell
cd D:\GitHub\AppleConverter
powershell -ExecutionPolicy Bypass -File scripts\Build-Installer.ps1 -InstallInno
```

This downloads FFmpeg into `tools\ffmpeg`, publishes a self-contained app, and builds:

```text
artifacts\installer\AppleConverterSetup-0.1.0-win-x64.exe
```

To publish the self-contained app folder without compiling the installer:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\Build-Installer.ps1 -SkipInstaller
```

## Backend notes

Images use Magick.NET. HEIC/HEIF support depends on the ImageMagick delegate support available in the package/runtime. If a file cannot be decoded, the app marks it failed with a readable reason and logs technical details.

Videos use FFmpeg. The app searches for FFmpeg in this order:

1. The path configured in Settings.
2. `tools\ffmpeg\ffmpeg.exe` next to the app.
3. `ffmpeg.exe` on the system `PATH`.

FFmpeg binaries are not committed to this repository. `scripts\Get-FFmpeg.ps1` downloads a Windows FFmpeg build and places `ffmpeg.exe` and `ffprobe.exe` under `tools\ffmpeg`; published and installer builds copy that folder into the app. The default script URL uses a GPL FFmpeg build, so review FFmpeg licensing and add third-party notices before public distribution.

The WinUI app project sets `WindowsAppSDKSelfContained=true`, and installer publishing uses `--self-contained true`, so release builds carry the Windows App SDK files and .NET runtime files beside the app executable.

## GitHub Actions

`build.yml` runs on pushes to `main`, pull requests to `main`, and manual dispatch. It restores, builds Release, runs tests, downloads FFmpeg, publishes the app self-contained, and uploads an artifact named with the app version and commit hash.

`release-draft.yml` runs on pushes to `main` and manual dispatch. It restores, builds, tests, installs Inno Setup, downloads FFmpeg, publishes, builds an `.exe` installer, zips the app folder, and creates or updates a draft GitHub Release with `softprops/action-gh-release@v2`. The release is always draft and prerelease; it is not published automatically.

## Known limitations

- FFmpeg is bundled into installer/publish outputs by the build script, but binaries remain excluded from source control.
- HEIC/HEIF conversion depends on Magick.NET/ImageMagick codec support on the target runtime.
- Frame extraction collision handling is conservative for sequence patterns.
- Theme changes are saved immediately and applied on next app launch.
- The UI is an MVP implementation ready for screenshots, final branding assets, signing, and release polish.

## Roadmap

- Add code signing for the installer.
- Add optional MSIX/MSIXBundle packaging.
- Add FFmpeg license display and third-party notices before public release.
- Add thumbnail previews and richer per-file details.
- Add per-Live-Photo group controls in the queue.
- Add ffprobe-based duration detection before conversion starts.
- Add more image backend fallbacks if needed.
- Add app icon, screenshots, and final branding assets.

## License

License placeholder. Choose the final license before publishing.
