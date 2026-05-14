; Sboard 접속기 - InnoSetup 스크립트
; 빌드 순서:
;   1. pyinstaller "Sboard 접속기.spec"          (dist\Sboard 접속기.exe 생성)
;   2. iscc "Sboard 접속기.iss"                   (Setup 파일 생성)

#define MyAppName "Sboard 접속기"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "류호준"
#define MyAppURL "https://github.com/c-closed/sal"
#define MyAppExeName "Sboard 접속기.exe"

[Setup]
AppId={{B8F7A3E2-1D4C-4A9E-8B6F-3C2D5E7A9F1B}
AppName={#MyAppName}
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
UninstallDisplayIcon={app}\icon.ico
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
Source: "dist\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Sboard 접속기 실행"; Flags: postinstall nowait skipifsilent unchecked

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im {#MyAppExeName}"; Flags: runhidden
