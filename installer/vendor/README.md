# Vendor binaries

`ffmpeg-bin/` here holds `ffmpeg.exe` and `ffprobe.exe`, bundled into the installer so
the installed Sentinel CMS never needs internet access to fetch them at first run
(important for an airgapped/closed network deployment).

Not committed to git (see `.gitignore` - `installer/vendor/`) since they're ~260MB.
`installer/build.ps1` expects them to already be here before building the installer.

## Re-fetching

```bash
mkdir -p installer/vendor/ffmpeg-bin
curl -L -o /tmp/ffmpeg.zip  https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-win-64.zip
curl -L -o /tmp/ffprobe.zip https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-win-64.zip
cd installer/vendor/ffmpeg-bin && unzip -o /tmp/ffmpeg.zip && unzip -o /tmp/ffprobe.zip
```

Same source `DigitalSignage.CMS`'s `FFmpegProvisioner` falls back to downloading
from at runtime if these files are ever missing - this just pre-stages them so
that fallback never has to trigger on an installed system.
