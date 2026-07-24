#define MyAppName "Bubbles CMD"
#define MyAppVersion "0.0.2"
#define MyAppPublisher "BubblesTheDev"
#define MyAppExeName "BubblesCmd.App.exe"
#define PublishDir "..\artifacts\publish\bubbles-cmd-0.0.2-win-x64"

[Setup]
AppId={{A40BA061-4C76-437A-9E3D-1A8767A0F6C5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Bubbles CMD
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=bubbles-cmd-0.0.2-setup
SetupIconFile=..\src\BubblesCmd.App\Assets\bubbles.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a Desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Bubbles CMD"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\Assets\bubbles.ico"
Name: "{autodesktop}\Bubbles CMD"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\Assets\bubbles.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Bubbles CMD"; Flags: nowait postinstall skipifsilent
