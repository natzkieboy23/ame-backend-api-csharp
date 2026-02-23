; ============================================================
;  AME Inventory Management — Inno Setup Installer Script
;  Requires: Inno Setup 6+ — https://jrsoftware.org/isdl.php
;
;  HOW TO BUILD:
;    1. Run publish.ps1 first  (creates the publish\ folder)
;    2. Open this file in Inno Setup IDE
;    3. Click Build > Compile  (or press F9)
;    Output: installer-output\AME-Inventory-Setup-1.0.0.exe
; ============================================================

#define AppName         "AME Inventory Management"
#define AppVersion      "1.0.0"
#define AppPublisher    "AME"
#define AppURL          "http://localhost:5141"
#define AppExe          "InventoryApi.exe"
#define ServiceName     "AME-InventoryAPI"
#define ServiceDisplay  "AME Inventory API"

; ── Setup metadata ──────────────────────────────────────────
[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName={autopf}\AME Inventory
DefaultGroupName=AME Inventory
OutputDir=installer-output
OutputBaseFilename=AME-Inventory-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
; Require admin — needed to install Windows Service and write to Program Files
PrivilegesRequired=admin
; Show a "restart required" page if needed
RestartIfNeededByRun=no
; Installer icon (optional — comment out if you don't have one)
; SetupIconFile=assets\icon.ico
WizardStyle=modern

; ── Files ──────────────────────────────────────────────────
[Files]
; Main executable — built by publish.ps1
Source: "publish\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

; Config file — only install if not already present (preserves user settings on upgrade)
Source: "publish\appsettings.json"; DestDir: "{app}"; Flags: onlyifdoesntexist

; ── Start Menu / Desktop shortcuts ─────────────────────────
[Icons]
; Desktop shortcut → launches interactive Sync Console in a cmd window
Name: "{commondesktop}\AME Sync Console";    Filename: "{app}\{#AppExe}"; Parameters: "--sync"; WorkingDir: "{app}"; Comment: "AME QuickBooks Sync Console"

; Start Menu shortcuts
Name: "{group}\AME Sync Console";            Filename: "{app}\{#AppExe}"; Parameters: "--sync"; WorkingDir: "{app}"; Comment: "AME QuickBooks Sync Console"
Name: "{group}\AME Inventory API (Swagger)"; Filename: "{#AppURL}";       Flags: shellexec;      Comment: "Open API in browser"
Name: "{group}\Uninstall AME Inventory";     Filename: "{uninstallexe}"

; ── Pascal code: install / uninstall Windows Service ───────
[Code]

{ ── Helper: run sc.exe silently ─────────────────────────── }
procedure RunSc(Params: string);
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), Params, '', SW_HIDE,
       ewWaitUntilTerminated, ResultCode);
end;

{ ── After files are copied: register and start the service ─ }
procedure CurStepChanged(CurStep: TSetupStep);
var
  BinPath: string;
begin
  if CurStep = ssPostInstall then
  begin
    BinPath := '"' + ExpandConstant('{app}') + '\{#AppExe}"';

    { Remove any existing service first (safe on clean install too) }
    RunSc('stop "' + '{#ServiceName}' + '"');
    RunSc('delete "' + '{#ServiceName}' + '"');

    { Register as auto-start service }
    RunSc('create "' + '{#ServiceName}' + '"'
        + ' binPath= ' + BinPath
        + ' start= auto'
        + ' DisplayName= "' + '{#ServiceDisplay}' + '"');

    { Set description visible in Services panel }
    RunSc('description "' + '{#ServiceName}' + '" "AME Inventory REST API (Swagger on http://localhost:5141)"');

    { Start immediately }
    RunSc('start "' + '{#ServiceName}' + '"');
  end;
end;

{ ── On uninstall: stop and remove the service ──────────── }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RunSc('stop "' + '{#ServiceName}' + '"');
    RunSc('delete "' + '{#ServiceName}' + '"');
  end;
end;

; ── Finish page message ─────────────────────────────────────
[Messages]
FinishedLabel=Setup has finished installing {#AppName}.%n%n  API is running at:  http://localhost:5141%n  Swagger UI opens at the same address.%n%n  To sync with QuickBooks, use the [AME Sync Console] shortcut on your desktop.
