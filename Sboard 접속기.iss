; Sboard 접속기 - InnoSetup 스크립트
; 빌드 순서:
;   1. pyinstaller "Sboard 접속기.spec"          (dist\Sboard 접속기.exe 생성)
;   2. iscc "Sboard 접속기.iss"                   (Setup 파일 생성)

#define MyAppName "Sboard 접속기"
#define MyAppVersion "2.1.0"
#define MyAppPublisher "류호준"
#define MyAppURL "https://github.com/c-closed/sal"
#define MyAppExeName "Sboard 접속기.exe"

[Setup]
AppId={{B8F7A3E2-1D4C-4A9E-8B6F-3C2D5E7A9F1B}
AppName={#MyAppName}
AppVerName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName=c-closed
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=Sboard_Setup
SetupIconFile=icon.ico
UninstallDisplayIcon={localappdata}\{#MyAppName}\icon.ico
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
DisableWelcomePage=no
LanguageDetectionMethod=uilanguage

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕 화면에 바로 가기 만들기"; GroupDescription: "바로 가기:"; Flags: checkedonce

[Files]
Source: "dist\{#MyAppName}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "dist\updater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "icon.ico"; DestDir: "{localappdata}\{#MyAppName}"; Flags: ignoreversion
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Sboard 접속기 실행"; Flags: postinstall nowait skipifsilent unchecked

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im {#MyAppExeName}"; Flags: runhidden

[Code]
function CompareVersion(V1, V2: string): Integer;
var
  P1, P2: Integer;
  N1, N2: Integer;
begin
  Result := 0;
  while (V1 <> '') or (V2 <> '') do
  begin
    P1 := Pos('.', V1);
    if P1 = 0 then P1 := Length(V1) + 1;
    P2 := Pos('.', V2);
    if P2 = 0 then P2 := Length(V2) + 1;

    if V1 = '' then N1 := 0 else N1 := StrToIntDef(Copy(V1, 1, P1 - 1), 0);
    if V2 = '' then N2 := 0 else N2 := StrToIntDef(Copy(V2, 1, P2 - 1), 0);

    if N1 < N2 then begin Result := -1; Exit; end;
    if N1 > N2 then begin Result := 1; Exit; end;

    if V1 = '' then V1 := '' else V1 := Copy(V1, P1 + 1, Length(V1));
    if V2 = '' then V2 := '' else V2 := Copy(V2, P2 + 1, Length(V2));
  end;
end;

function InitializeSetup: Boolean;
var
  PrevVersion: string;
begin
  Result := True;
  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{B8F7A3E2-1D4C-4A9E-8B6F-3C2D5E7A9F1B}_is1', 'DisplayVersion', PrevVersion) or
     RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{B8F7A3E2-1D4C-4A9E-8B6F-3C2D5E7A9F1B}_is1', 'DisplayVersion', PrevVersion) then
  begin
    if CompareVersion(PrevVersion, '{#MyAppVersion}') >= 0 then
    begin
      MsgBox('이미 동일하거나 최신 버전(Sboard 접속기 ' + PrevVersion + ')이 설치되어 있습니다.' #13#13 '설치를 계속할 수 없습니다.', mbInformation, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    RegWriteStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{B8F7A3E2-1D4C-4A9E-8B6F-3C2D5E7A9F1B}_is1', 'DisplayVersion', '{#MyAppVersion}');
end;
