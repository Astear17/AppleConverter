# FFmpeg backend folder

Apple Converter looks for `ffmpeg.exe` in this folder at runtime:

```text
tools/ffmpeg/ffmpeg.exe
```

The repository does not commit FFmpeg binaries. For an app distribution, include
a license-compatible FFmpeg build here or ask the user to set an FFmpeg path in
Settings. The app also searches the system `PATH`.
