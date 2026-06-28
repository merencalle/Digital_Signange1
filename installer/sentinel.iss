; Sentinel - Mission-Ready Digital Signage, Built for the Army
; Inno Setup script. Build via installer\build.ps1 (which publishes the app,
; stages FFmpeg, then compiles this script) - don't run ISCC directly against
; a stale staging folder.

#define AppName "Sentinel"
#define AppPublisher "Sentinel Digital Signage"
#define AppExeName "DigitalSignage.CMS.exe"
#define ServiceName "Sentinel"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef StagingDir
  #define StagingDir "staging"
#endif

[Setup]
AppId={{B6E1A6B0-6B0A-4C7E-9B7D-2E1A0E0E5C1A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\Sentinel
DefaultGroupName=Sentinel
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=output
OutputBaseFilename=SentinelSetup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "firewall"; Description: "Open Windows Firewall ports 5109 (HTTP) and 5110 (HTTPS) for Sentinel"; Flags: checkedonce
Name: "defenderexclusion"; Description: "Exclude the Sentinel install folder from Windows Defender real-time scanning (recommended - speeds up FFmpeg/content operations)"; Flags: checkedonce

[Files]
Source: "{#StagingDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\Sentinel Admin Console"; Filename: "https://localhost:5110"
Name: "{group}\Uninstall Sentinel"; Filename: "{uninstallexe}"

[Run]
; Stop and remove any previous installation's service first (safe no-op if it doesn't exist).
Filename: "{sys}\sc.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden; StatusMsg: "Stopping any existing Sentinel service..."
Filename: "{sys}\sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden; StatusMsg: "Removing any existing Sentinel service..."

Filename: "{sys}\sc.exe"; Parameters: "create {#ServiceName} binPath= ""{app}\{#AppExeName}"" start= auto DisplayName= ""Sentinel Digital Signage CMS"""; Flags: runhidden; StatusMsg: "Registering the Sentinel Windows Service..."
Filename: "{sys}\sc.exe"; Parameters: "description {#ServiceName} ""Mission-Ready Digital Signage - Built for the Army"""; Flags: runhidden

Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -Command ""New-NetFirewallRule -DisplayName 'Sentinel HTTPS' -Direction Inbound -LocalPort 5110 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue; New-NetFirewallRule -DisplayName 'Sentinel HTTP' -Direction Inbound -LocalPort 5109 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue"""; Flags: runhidden; StatusMsg: "Opening firewall ports..."; Tasks: firewall

Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -Command ""Add-MpPreference -ExclusionPath '{app}' -ErrorAction SilentlyContinue"""; Flags: runhidden; StatusMsg: "Adding Windows Defender exclusion..."; Tasks: defenderexclusion

Filename: "{sys}\sc.exe"; Parameters: "start {#ServiceName}"; Flags: runhidden; StatusMsg: "Starting the Sentinel service..."

Filename: "https://localhost:5110"; Description: "Open Sentinel in your browser to finish setup"; Flags: postinstall shellexec skipifsilent

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden; RunOnceId: "StopService"
Filename: "{sys}\sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden; RunOnceId: "DeleteService"
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -Command ""Remove-NetFirewallRule -DisplayName 'Sentinel HTTPS' -ErrorAction SilentlyContinue; Remove-NetFirewallRule -DisplayName 'Sentinel HTTP' -ErrorAction SilentlyContinue"""; Flags: runhidden; RunOnceId: "RemoveFirewallRules"
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -Command ""Remove-MpPreference -ExclusionPath '{app}' -ErrorAction SilentlyContinue"""; Flags: runhidden; RunOnceId: "RemoveDefenderExclusion"

[UninstallDelete]
; Application binaries only. The data folder (App_Data: certs, backups, the
; database) and wwwroot/media (uploaded content) are deliberately left in
; place - re-running the installer later (an upgrade) should not destroy data.
Type: filesandordirs; Name: "{app}\ffmpeg-bin"

[Messages]
WelcomeLabel1=Welcome to the [name] Setup Wizard
WelcomeLabel2=This installs the Sentinel Digital Signage CMS as a Windows Service, opens the firewall ports it needs, and registers it to start automatically.%n%nOnce installed, your browser will open to the admin console, where a first-time setup wizard walks you through the rest (network type, certificates, and your first Player) based on how you're deploying it.
FinishedLabel=Setup has finished installing Sentinel on your computer. The Sentinel service is running.%n%nYour browser will open to https://localhost:5110 - the certificate warning on first launch is expected (a temporary certificate is used until you generate a real one in the Deployment Wizard). Log in with username "admin" and password "ChangeMe123!" - you'll be required to change it immediately.
