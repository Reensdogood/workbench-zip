# myzip

myzip is an independent, ad-free Windows x64 archive utility. The source code written for this project is licensed under the MIT License in [`LICENSE`](LICENSE).

## Features

- Open, list, test, and extract ZIP, 7z, RAR, TAR, and other formats supported by SharpCompress.
- Create ZIP and 7z archives with fast, balanced, and smaller-size presets.
- Extract password-protected archives when supported by the format/library.
- Drag and drop archives into the window.
- Open an archive immediately when Windows launches myzip with a file path (including ZIP double-click associations).
- Block archive path traversal and refuse links that could redirect extraction outside the destination.
- Open archive entries in their Windows default applications; files stage in a private temporary folder, and EXE/MSI launches require confirmation.
- Preview images, PDF, text, Office Open XML, and HWPX entries from the archive. Office and HWPX previews extract readable text and do not reproduce original page layout.
- Check archive entries before extracting or deleting them; an empty check selection extracts the full archive.
- Add files or remove checked entries, then save the edited contents as a new ZIP or 7z archive.
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

To create the Windows setup program, publish the personal build to `publish-personal` and compile [`installer/myzip.iss`](installer/myzip.iss) with Inno Setup 7. The installer defaults to `{autopf}\myzip` (on x64 Windows, `C:\Program Files\myzip`) and creates a Start menu shortcut. A desktop shortcut is optional.

When myzip is selected as the `.zip` default app in Windows Settings, Windows passes the double-clicked archive path to the app. myzip reads that argument and opens the archive directly. Double-clicking a file inside an archive extracts it to a private temporary folder and opens it with its Windows default application. Files remain available in `%TEMP%\myzip-open` and folders older than seven days are cleaned up when myzip next opens. EXE/MSI entries require explicit confirmation; EXE packages and their archive contents are staged together, while MSI files are launched through Windows Installer in interactive mode. Installation elevation prompts are handled by Windows. Viewer and codec support depends on the applications installed on the PC.

## Folder layout

- `myzip.exe`: app and bundled .NET runtime.
- `UnEGG64.dll`: optional, separately licensed ALZ/EGG module; only present in personal builds where supplied.
- `docs/`: user and third-party license notices in packaged builds.
- `assets/`: source icon assets.

No database is required.
