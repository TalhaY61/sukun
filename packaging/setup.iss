#define AppVersion "0.1.0-beta.2"
[Setup]
AppId={{2150CBCA-4F88-48EF-BEC6-F3B57A61E2AC}
AppName=Sükûn
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Sukun
DefaultGroupName=Sükûn
PrivilegesRequired=lowest
MinVersion=10.0.19041
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
SetupIconFile=..\src\Sukun.Windows\Assets\Brand\app.ico
OutputDir=..\artifacts
OutputBaseFilename=Sukun-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Sukun.exe
CloseApplications=yes
RestartApplications=no
[Files]
Source: "..\artifacts\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
[Icons]
Name: "{group}\Sükûn"; Filename: "{app}\Sukun.exe"
[Run]
Filename: "{app}\Sukun.exe"; Description: "Sükûn’nı aç"; Flags: nowait postinstall skipifsilent
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "Sukun"; Flags: uninsdeletevalue
; User preferences/timetables intentionally survive uninstall. No default autostart.
