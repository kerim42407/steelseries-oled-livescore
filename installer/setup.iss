; Inno Setup script for OledLiveScore.
; Build:  "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" installer\setup.iss
; Output: dist\OledLiveScore-Setup.exe

#define AppName "OledLiveScore"
#define AppVersion "1.1.0"
#define AppPublisher "Kerim Mandaci"
#define AppExe "OledLiveScore.exe"
#define AppUrl "https://github.com/kerim42407/steelseries-oled-livescore"

[Setup]
AppId={{8F3A1C7E-2B94-4E6D-9A5F-1D0C7B2E44A1}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppSupportURL={#AppUrl}
AppPublisherURL={#AppUrl}
DefaultDirName={localappdata}\Programs\{#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=OledLiveScore-Setup
SetupIconFile=..\src\OledLiveScore.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\src\bin\Release\net48\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Remove the app's "Start with Windows" entry when uninstalling.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "{#AppName}"; ValueType: none; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch OledLiveScore"; Flags: nowait postinstall skipifsilent
; In-app update: the running copy started us silently, so put it back in the tray.
Filename: "{app}\{#AppExe}"; Parameters: "--silent"; Flags: nowait; Check: WizardSilent
