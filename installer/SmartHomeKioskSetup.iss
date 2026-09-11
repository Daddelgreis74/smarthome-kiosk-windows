#define MyAppName "SmartHome Kiosk"
#define MyAppVersion "1.0.4"
#define MyAppPublisher "SmartHome"
#define MyAppExeName "SmartHomeKiosk.exe"

[Setup]
AppId={{D37E83F1-92A5-4E87-9A1B-F3C1E8402D9A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SmartHomeKiosk
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
UsedUserAreasWarning=no
OutputDir=..\dist
OutputBaseFilename=SmartHomeKiosk-Setup
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "SmartHome Kiosk automatisch mit Windows starten"; GroupDescription: "Systemstart:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SmartHomeKiosk"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: autostart; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SmartHomeKiosk"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; Check: NeedsWebView2; StatusMsg: "Installiere Microsoft Edge WebView2 Runtime..."
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall

[Code]
function NeedsWebView2(): Boolean;
var
  version: String;
begin
  // Prüft, ob WebView2 Runtime bereits installiert ist (Systemweit oder Per-User)
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', version) and (version <> '') then
  begin
    Result := False;
    Exit;
  end;

  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', version) and (version <> '') then
  begin
    Result := False;
    Exit;
  end;

  if RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', version) and (version <> '') then
  begin
    Result := False;
    Exit;
  end;

  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  appDataDir: String;
  localAppDataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    if MsgBox('Möchten Sie auch die gespeicherten Einstellungen (URL, PIN) und Cache-Dateien von SmartHome Kiosk entfernen?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      appDataDir := ExpandConstant('{userappdata}\SmartHomeKiosk');
      localAppDataDir := ExpandConstant('{localappdata}\SmartHomeKiosk');

      if DirExists(appDataDir) then
        DelTree(appDataDir, True, True, True);

      if DirExists(localAppDataDir) then
        DelTree(localAppDataDir, True, True, True);
    end;
  end;
end;
