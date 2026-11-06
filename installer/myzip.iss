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
