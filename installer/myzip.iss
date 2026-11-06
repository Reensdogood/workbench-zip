#define MyAppName "myzip"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "myzip"

[Setup]
AppId={{8d14063e-6c66-4f22-818c-0f7a8420280b}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\myzip
DefaultGroupName=myzip
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
OutputDir=..\..
OutputBaseFilename=myzip-setup-win-x64
SetupIconFile=..\assets\myzip.ico
UninstallDisplayIcon={app}\myzip.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
Uninstallable=yes
DisableProgramGroupPage=yes

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕 화면에 바로 가기 만들기"; GroupDescription: "추가 바로 가기:"; Flags: unchecked

[Files]
Source: "..\publish-personal\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\myzip"; Filename: "{app}\myzip.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\myzip"; Filename: "{app}\myzip.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\myzip.exe"; Description: "myzip 실행"; Flags: postinstall nowait skipifsilent

; Explorer context menu: compress one selected file or folder.
[Registry]
Root: HKCR; Subkey: "*\shell\myzip_compress"; ValueType: string; ValueData: "myzip으로 압축"; Flags: uninsdeletekey
Root: HKCR; Subkey: "*\shell\myzip_compress"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "*\shell\myzip_compress"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "*\shell\myzip_compress\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --compress ""%1"""
Root: HKCR; Subkey: "Directory\shell\myzip_compress"; ValueType: string; ValueData: "myzip으로 압축"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\shell\myzip_compress"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "Directory\shell\myzip_compress"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "Directory\shell\myzip_compress\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --compress ""%1"""

; Extract archive files into a folder named after the archive, beside the archive.
Root: HKCR; Subkey: "SystemFileAssociations\.zip\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.zip\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.zip\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.zip\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.7z\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.7z\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.7z\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.7z\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.rar\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.rar\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.rar\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.rar\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.egg\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.egg\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.egg\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.egg\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.alz\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.alz\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.alz\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.alz\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.tar\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.tar\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.tar\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.tar\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.gz\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.gz\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.gz\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.gz\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.bz2\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.bz2\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.bz2\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.bz2\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.xz\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.xz\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.xz\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.xz\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.zst\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.zst\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.zst\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.zst\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.cab\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.cab\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.cab\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.cab\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.arj\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.arj\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.arj\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.arj\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.ace\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.ace\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.ace\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.ace\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.lzh\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.lzh\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.lzh\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.lzh\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.arc\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.arc\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.arc\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.arc\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.iso\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.iso\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.iso\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.iso\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.wim\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.wim\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.wim\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.wim\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
Root: HKCR; Subkey: "SystemFileAssociations\.001\shell\myzip_extract"; ValueType: string; ValueData: "myzip으로 압축 해제"; Flags: uninsdeletekey
Root: HKCR; Subkey: "SystemFileAssociations\.001\shell\myzip_extract"; ValueName: "Icon"; ValueType: string; ValueData: "{app}\myzip.exe,0"
Root: HKCR; Subkey: "SystemFileAssociations\.001\shell\myzip_extract"; ValueName: "MultiSelectModel"; ValueType: string; ValueData: "Single"
Root: HKCR; Subkey: "SystemFileAssociations\.001\shell\myzip_extract\command"; ValueType: string; ValueData: """{app}\myzip.exe"" --extract ""%1"""
