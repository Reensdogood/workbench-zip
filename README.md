# myzip

myzip is an independent, ad-free Windows x64 archive utility. The source code written for this project is licensed under the MIT License in [`LICENSE`](LICENSE).

## Features

- Open, list, test, and extract ZIP, 7z, RAR, TAR, and other formats supported by SharpCompress.
- Create ZIP and 7z archives with fast, balanced, and smaller-size presets.
- Extract password-protected archives when supported by the format/library.
- Drag and drop archives into the window.
- Open an archive immediately when Windows launches myzip with a file path (including ZIP double-click associations).
- Block archive path traversal and refuse links that could redirect extraction outside the destination.
- Preview text and PDF files, common images, Office Open XML documents, and supported video/audio formats from inside an archive or when opened directly.
- Optional ALZ/EGG extraction support through ESTsoft's unmodified UnEGG64 module. This module is not included in this repository or covered by the project's MIT license.

## License and third-party components

The MIT license applies to myzip's original source code and icon only. It does not relicense third-party components. SharpCompress is distributed under its own MIT license; its notice is in [`licenses/SharpCompress-MIT-LICENSE.txt`](licenses/SharpCompress-MIT-LICENSE.txt). Microsoft WebView2 and .NET runtime components keep their respective Microsoft notices; packaged notices are in `docs/licenses/`.

### Optional ALZ/EGG support: personal use only

The optional `UnEGG64.dll` is proprietary third-party software from ESTsoft and is deliberately excluded from this public repository and its MIT license. The project does not modify or redistribute this DLL here. If you choose to enable ALZ/EGG extraction, obtain the official module and follow its current license. This configuration is intended for your own personal, non-commercial use; commercial use or redistribution requires checking the current ESTsoft terms and obtaining any required permission. The app does not create ALZ/EGG archives.

Official ESTsoft information: https://altools.co.kr/service/FAQ?no=262

## Build

Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet publish .\myzip.csproj -c Release -r win-x64 --self-contained true
```

The self-contained build emits a single `myzip.exe`. To enable optional ALZ/EGG support for personal use, obtain the official module and build with `-p:IncludeUnEggModule=true`; the module must be placed at `vendor/estsoft/UnEGG64.dll` and is copied separately next to `myzip.exe`. Without that module, other supported archive formats remain available.

PDF previews use the Microsoft Edge WebView2 Runtime, normally installed with current Windows 10/11 systems. Video playback uses Windows media components and works only for codecs supported by that PC.

When myzip is selected as the `.zip` default app in Windows Settings, Windows passes the double-clicked archive path to the app. myzip reads that argument and opens the archive directly. Image previews include PNG, JPEG, BMP, GIF, and TIFF; document previews include PDF, plain text, and the text content of DOCX/XLSX/PPTX. Video/audio preview uses Windows' installed codecs and may not support every codec or container.

## Folder layout

- `myzip.exe`: app and bundled .NET runtime.
- `UnEGG64.dll`: optional, separately licensed ALZ/EGG module; only present in personal builds where supplied.
- `docs/`: user and third-party license notices in packaged builds.
- `assets/`: source icon assets.

No database is required.
