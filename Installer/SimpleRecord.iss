; ============================================================================
; Simple Record - Inno Setup installer script
;
; What this does: packages the already-published app into one file called
; "setup.exe". Running that file installs Simple Record into Program Files,
; adds a Start Menu shortcut (and, if the user ticks the box, a desktop
; shortcut), registers it in Windows' "Add or Remove Programs" list with a
; proper uninstaller, and optionally launches the app when setup finishes.
;
; This file does NOT build the app itself - it only packages a build that
; already exists. You must run "dotnet publish" first (see the main
; README.md, section 5, "Make a shareable installer (setup.exe)") so the
; folder referenced below by MyPublishDir actually exists before compiling
; this script.
;
; HOW TO BUILD setup.exe FROM THIS SCRIPT:
;   1. Install Inno Setup (free): https://jrsoftware.org/isdl.php
;   2. Publish the app first - see README.md section 5 for the exact
;      "dotnet publish" command to run.
;   3. Open this file (Installer\SimpleRecord.iss) in the Inno Setup
;      Compiler - either double-click it (if Inno Setup is installed, it
;      associates .iss files automatically) or open Inno Setup and use
;      File > Open.
;   4. Click the green "Compile" (Run/Play) button, or press F9 (Ctrl+F9 in
;      some Inno Setup versions).
;   5. When it finishes, setup.exe appears in Installer\Output\setup.exe.
;
; If you bump the app's version in simple-record-by-mrsmith9.csproj, update
; MyAppVersion below to match before compiling, so the installer and the
; app report the same version number to Windows.
; ============================================================================

#define MyAppName "Simple Record"
#define MyAppVersion "0.9.3"
#define MyAppPublisher "Kyle Smith"
#define MyAppExeName "simple-record-by-mrsmith9.exe"

; This is where "dotnet publish" puts the finished build (see README.md,
; section 5). If you ever change the publish command or target a different
; runtime identifier than win-x64, update this path to match - otherwise
; Inno Setup will fail to compile with a "file not found" error.
#define MyPublishDir "..\src\simple-record-by-mrsmith9\bin\x64\Release\net8.0-windows\win-x64\publish"

[Setup]
; This ID stays the same across every version - it's how Windows
; recognizes "this is still Simple Record" when someone installs a newer
; version over an older one, and lets it replace the old one cleanly
; instead of ending up with two separate entries in "Add or Remove
; Programs". Never change this value once it's set.
AppId={{9C93CCC6-BAC0-482B-A0CD-F04E32D81D4B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppUpdatesURL=https://github.com/MrSmith9/simple-record
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Output
OutputBaseFilename=setup
SetupIconFile=..\src\simple-record-by-mrsmith9\Assets\AppIcon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; The published build is 64-bit only (see README.md), so the installer
; refuses to run on a 32-bit Windows PC rather than installing something
; that wouldn't work.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
; Copies every file dotnet published (the .exe, its .dll files, the
; bundled .NET runtime, etc.) into the install folder. "recursesubdirs"
; and "createallsubdirs" make sure nothing is left behind if the publish
; output ever grows subfolders.
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Offers to launch the app right after Setup finishes - unticked by
; default is not possible here (Inno always shows it ticked), but the
; user can still untick it on the final Setup page before clicking Finish.
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
