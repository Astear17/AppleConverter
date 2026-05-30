#define RepoRoot GetEnv("APPLECONVERTER_REPO_ROOT")
#define SourceDir GetEnv("APPLECONVERTER_PUBLISH_DIR")
#define OutputDir GetEnv("APPLECONVERTER_INSTALLER_DIR")

#if RepoRoot == ""
  #define RepoRoot ".."
#endif
#if SourceDir == ""
  #define SourceDir "..\artifacts\publish\AppleConverter"
#endif
#if OutputDir == ""
  #define OutputDir "..\artifacts\installer"
#endif

[Setup]
AppId={{7DE2C7D9-6F5A-4A6E-9A4A-A3F39A2B8F11}
AppName=Apple Converter
AppVersion=0.1.0
AppPublisher=Apple Converter Contributors
AppPublisherURL=https://github.com/your-org/AppleConverter
AppSupportURL=https://github.com/your-org/AppleConverter/issues
AppUpdatesURL=https://github.com/your-org/AppleConverter/releases
DefaultDirName={autopf}\Apple Converter
DefaultGroupName=Apple Converter
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=AppleConverterSetup-0.1.0-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#RepoRoot}\src\AppleLegacyMediaConverter\Assets\AppIcon.ico
UninstallDisplayIcon={app}\AppleLegacyMediaConverter.exe
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
ChangesAssociations=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Apple Converter"; Filename: "{app}\AppleLegacyMediaConverter.exe"
Name: "{autodesktop}\Apple Converter"; Filename: "{app}\AppleLegacyMediaConverter.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AppleLegacyMediaConverter.exe"; Description: "{cm:LaunchProgram,Apple Converter}"; Flags: nowait postinstall skipifsilent
