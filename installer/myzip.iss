#define MyAppName "myzip"
#define MyAppVersion "1.0.6"
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
Source: "..\publish-public\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\myzip"; Filename: "{app}\myzip.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\myzip"; Filename: "{app}\myzip.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "ms-settings:defaultapps?registeredAppMachine=myzip"; Description: "선택한 압축 형식의 기본 앱 설정"; Flags: postinstall shellexec skipifsilent; Check: HasSelectedAssociations
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
; Windows 10/11 lets users choose the default handler in Settings. Register Open With and Capabilities; do not overwrite protected UserChoice.
; Windows requires user confirmation for default handlers. The installer registers support and opens Default Apps.
Root: HKLM; Subkey: "Software\myzip\Capabilities"; ValueName: "ApplicationName"; ValueType: string; ValueData: "myzip"; Flags: uninsdeletekey; Check: HasSelectedAssociations
Root: HKLM; Subkey: "Software\myzip\Capabilities"; ValueName: "ApplicationDescription"; ValueType: string; ValueData: "압축 파일 열기 및 관리"; Check: HasSelectedAssociations
Root: HKLM; Subkey: "Software\RegisteredApplications"; ValueName: "myzip"; ValueType: string; ValueData: "Software\myzip\Capabilities"; Flags: uninsdeletevalue; Check: HasSelectedAssociations
Root: HKLM; Subkey: "Software\myzip\Capabilities\FileAssociations"; ValueName: ".zip"; ValueType: string; ValueData: "myzip.zip"; Check: ShouldAssociate('zip')
Root: HKCR; Subkey: "myzip.zip"; ValueType: string; ValueData: "myzip ZIP archive"; Flags: uninsdeletekey; Check: ShouldAssociate('zip')
Root: HKCR; Subkey: "myzip.zip\DefaultIcon"; ValueType: string; ValueData: "{app}\myzip.exe,0"; Check: ShouldAssociate('zip')
Root: HKCR; Subkey: "myzip.zip\shell\open\command"; ValueType: string; ValueData: """{app}\myzip.exe"" ""%1"""; Check: ShouldAssociate('zip')
Root: HKCR; Subkey: ".zip\OpenWithProgids"; ValueName: "myzip.zip"; ValueType: none; Flags: uninsdeletevalue; Check: ShouldAssociate('zip')
Root: HKLM; Subkey: "Software\myzip\Capabilities\FileAssociations"; ValueName: ".7z"; ValueType: string; ValueData: "myzip.7z"; Check: ShouldAssociate('7z')
Root: HKCR; Subkey: "myzip.7z"; ValueType: string; ValueData: "myzip 7Z archive"; Flags: uninsdeletekey; Check: ShouldAssociate('7z')
Root: HKCR; Subkey: "myzip.7z\DefaultIcon"; ValueType: string; ValueData: "{app}\myzip.exe,0"; Check: ShouldAssociate('7z')
Root: HKCR; Subkey: "myzip.7z\shell\open\command"; ValueType: string; ValueData: """{app}\myzip.exe"" ""%1"""; Check: ShouldAssociate('7z')
Root: HKCR; Subkey: ".7z\OpenWithProgids"; ValueName: "myzip.7z"; ValueType: none; Flags: uninsdeletevalue; Check: ShouldAssociate('7z')
Root: HKLM; Subkey: "Software\myzip\Capabilities\FileAssociations"; ValueName: ".rar"; ValueType: string; ValueData: "myzip.rar"; Check: ShouldAssociate('rar')
Root: HKCR; Subkey: "myzip.rar"; ValueType: string; ValueData: "myzip RAR archive"; Flags: uninsdeletekey; Check: ShouldAssociate('rar')
Root: HKCR; Subkey: "myzip.rar\DefaultIcon"; ValueType: string; ValueData: "{app}\myzip.exe,0"; Check: ShouldAssociate('rar')
Root: HKCR; Subkey: "myzip.rar\shell\open\command"; ValueType: string; ValueData: """{app}\myzip.exe"" ""%1"""; Check: ShouldAssociate('rar')
Root: HKCR; Subkey: ".rar\OpenWithProgids"; ValueName: "myzip.rar"; ValueType: none; Flags: uninsdeletevalue; Check: ShouldAssociate('rar')
Root: HKLM; Subkey: "Software\myzip\Capabilities\FileAssociations"; ValueName: ".tar"; ValueType: string; ValueData: "myzip.tar"; Check: ShouldAssociate('tar')
Root: HKCR; Subkey: "myzip.tar"; ValueType: string; ValueData: "myzip TAR archive"; Flags: uninsdeletekey; Check: ShouldAssociate('tar')
Root: HKCR; Subkey: "myzip.tar\DefaultIcon"; ValueType: string; ValueData: "{app}\myzip.exe,0"; Check: ShouldAssociate('tar')
Root: HKCR; Subkey: "myzip.tar\shell\open\command"; ValueType: string; ValueData: """{app}\myzip.exe"" ""%1"""; Check: ShouldAssociate('tar')
Root: HKCR; Subkey: ".tar\OpenWithProgids"; ValueName: "myzip.tar"; ValueType: none; Flags: uninsdeletevalue; Check: ShouldAssociate('tar')

[Code]
var
  AssocPage: TWizardPage;
  AssocAllRadio, AssocSelectRadio, AssocNoneRadio: TNewRadioButton;
  AssocChecks: array[0..3] of TNewCheckBox;

function ArchiveExtension(Index: Integer): String;
begin
  case Index of
    0: Result := 'zip'; 1: Result := '7z';
    2: Result := 'rar'; 3: Result := 'tar';
  end;
end;

procedure UpdateAssociationChecks;
var I: Integer;
begin
  for I := 0 to 3 do
    AssocChecks[I].Enabled := AssocSelectRadio.Checked;
end;

procedure AssociationModeChanged(Sender: TObject);
begin
  UpdateAssociationChecks;
end;

procedure InitializeWizard;
var I: Integer;
begin
  AssocPage := CreateCustomPage(wpSelectTasks, '압축 파일 연결',
    'myzip에서 열 파일 형식을 선택하세요. 기본 앱 변경은 Windows 설정에서 확인할 수 있습니다.');
  AssocAllRadio := TNewRadioButton.Create(AssocPage);
  AssocAllRadio.Parent := AssocPage.Surface;
  AssocAllRadio.Caption := '전체 지원 형식 선택';
  AssocAllRadio.Checked := True;
  AssocAllRadio.Left := ScaleX(0); AssocAllRadio.Top := ScaleY(8);
  AssocAllRadio.Width := ScaleX(300); AssocAllRadio.OnClick := @AssociationModeChanged;
  AssocSelectRadio := TNewRadioButton.Create(AssocPage);
  AssocSelectRadio.Parent := AssocPage.Surface;
  AssocSelectRadio.Caption := '아래에서 직접 선택';
  AssocSelectRadio.Left := ScaleX(0); AssocSelectRadio.Top := ScaleY(38);
  AssocSelectRadio.Width := ScaleX(300); AssocSelectRadio.OnClick := @AssociationModeChanged;
  AssocNoneRadio := TNewRadioButton.Create(AssocPage);
  AssocNoneRadio.Parent := AssocPage.Surface;
  AssocNoneRadio.Caption := '파일 연결 안 함';
  AssocNoneRadio.Left := ScaleX(0); AssocNoneRadio.Top := ScaleY(68);
  AssocNoneRadio.Width := ScaleX(300); AssocNoneRadio.OnClick := @AssociationModeChanged;
  for I := 0 to 3 do begin
    AssocChecks[I] := TNewCheckBox.Create(AssocPage);
    AssocChecks[I].Parent := AssocPage.Surface;
    AssocChecks[I].Caption := '.' + ArchiveExtension(I);
    AssocChecks[I].Checked := True;
    AssocChecks[I].Enabled := False;
    AssocChecks[I].Left := ScaleX((I mod 2) * 150);
    AssocChecks[I].Top := ScaleY(112 + (I div 2) * 30);
    AssocChecks[I].Width := ScaleX(140);
  end;
end;

function ShouldAssociate(Ext: String): Boolean;
var I: Integer;
begin
  Result := False;
  if WizardSilent then exit;
  if AssocAllRadio.Checked then begin Result := True; exit; end;
  if not AssocSelectRadio.Checked then exit;
  for I := 0 to 3 do
    if CompareText(ArchiveExtension(I), Ext) = 0 then begin
      Result := AssocChecks[I].Checked; exit;
    end;
end;

function HasSelectedAssociations: Boolean;
var I: Integer;
begin
  Result := AssocAllRadio.Checked and not WizardSilent;
  if Result then exit;
  if WizardSilent or not AssocSelectRadio.Checked then exit;
  for I := 0 to 3 do
    if AssocChecks[I].Checked then begin Result := True; exit; end;
end;
