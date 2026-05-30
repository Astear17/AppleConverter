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

Requirements:

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
dotnet publish src\AppleLegacyMediaConverter\AppleLegacyMediaConverter.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained false -o artifacts\publish\AppleConverter
Compress-Archive -Path artifacts\publish\AppleConverter\* -DestinationPath artifacts\AppleConverter-win-x64.zip -Force
```

## Backend notes

Images use Magick.NET. HEIC/HEIF support depends on the ImageMagick delegate support available in the package/runtime. If a file cannot be decoded, the app marks it failed with a readable reason and logs technical details.

Videos use FFmpeg. The app searches for FFmpeg in this order:

1. The path configured in Settings.
2. `tools\ffmpeg\ffmpeg.exe` next to the app.
3. `ffmpeg.exe` on the system `PATH`.

FFmpeg binaries are not committed to this repository. Bundle a license-compatible FFmpeg build for distribution, or direct users to set the path in Settings.

## GitHub Actions

`build.yml` runs on pushes to `main`, pull requests to `main`, and manual dispatch. It restores, builds Release x64, runs tests, publishes the app, and uploads an artifact named with the app version and commit hash.

`release-draft.yml` runs on pushes to `main` and manual dispatch. It restores, builds, tests, publishes, zips the app, and creates or updates a draft GitHub Release with `softprops/action-gh-release@v2`. The release is always draft and prerelease; it is not published automatically.

## Known limitations

- The MVP does not ship FFmpeg binaries in the repository.
- HEIC/HEIF conversion depends on Magick.NET/ImageMagick codec support on the target runtime.
- Frame extraction collision handling is conservative for sequence patterns.
- Theme changes are saved immediately and applied on next app launch.
- The UI is an MVP implementation ready for refinement, screenshots, icons, installer work, and final release polish.

## Roadmap

- Add signed installer/MSIX packaging.
- Add bundled FFmpeg acquisition or packaging flow with license review.
- Add thumbnail previews and richer per-file details.
- Add per-Live-Photo group controls in the queue.
- Add ffprobe-based duration detection before conversion starts.
- Add more image backend fallbacks if needed.
- Add app icon, screenshots, and final branding assets.

## License

License placeholder. Choose the final license before publishing.
