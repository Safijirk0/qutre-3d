; Qutre 3D — Windows installer (Inno Setup 6)
; Build:  ISCC.exe installer\windows\qutre3d.iss

#define MyAppName "Qutre 3D"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "Qutre 3D"
#define MyAppExeName "Qutre3D.exe"
#define MyAppId "{{8E1D0A8E-3F5E-4E2A-9E4B-6B7C2F5D9A11}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Qutre 3D
DefaultGroupName=Qutre 3D
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\dist\installers
OutputBaseFilename=Qutre3D-{#MyAppVersion}-win-x64-setup
SetupIconFile=..\..\assets\logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
russian.CreateDesktopIcon=Создать ярлык на рабочем столе
english.CreateDesktopIcon=Create a desktop shortcut
russian.LaunchApp=Запустить Qutre 3D
english.LaunchApp=Launch Qutre 3D
russian.AdditionalIcons=Дополнительно:
english.AdditionalIcons=Additional icons:

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\..\dist\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\examples\*.qutre"; DestDir: "{app}\examples"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Qutre 3D"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Qutre 3D"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  SettingsDir: String;
  SettingsFile: String;
  LanguageCode: String;
  Json: String;
begin
  if CurStep = ssPostInstall then
  begin
    SettingsDir := ExpandConstant('{userappdata}\Qutre3D');
    SettingsFile := SettingsDir + '\settings.json';

    // Не перезаписываем выбор языка, сделанный в самом приложении.
    if not FileExists(SettingsFile) then
    begin
      if ActiveLanguage() = 'russian' then
        LanguageCode := 'ru'
      else
        LanguageCode := 'en';

      Json := '{"Language":"' + LanguageCode + '","LastDirectory":""}';

      ForceDirectories(SettingsDir);
      SaveStringToFile(SettingsFile, Json, False);
    end;
  end;
end;
