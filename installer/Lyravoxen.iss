#define MyAppName "Lyravoxen"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Lyravoxen"

[Setup]
AppId={{A65D1E0C-4C2A-4D79-9D73-9F3B7D26A8C1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Lyravoxen
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts\installer
OutputBaseFilename=Lyravoxen-Setup-Windows-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\Lyravoxen.exe
SetupLogging=yes

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "..\artifacts\windows-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Lyravoxen"; Filename: "{app}\Lyravoxen.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Lyravoxen"; Filename: "{app}\Lyravoxen.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Lyravoxen.exe"; Description: "Abrir Lyravoxen"; Flags: postinstall nowait skipifsilent
