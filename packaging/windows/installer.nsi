; NOVA Workspace - Windows installer (NSIS 3.x, Unicode).
;
; Per-user install: no administrator prompt, no UAC, works the same on
; Windows 7 SP1 through Windows 11 and on x64, x86 and ARM64 payloads.
;
; Build (see scripts/Build-NovaInstallers.ps1):
;   makensis /DARCH=x64 /DSRCDIR=...\NOVAWorkspace /DICONDIR=...\assets\nova ^
;            /DHASDESK=1 /O...\NOVAWorkspace-Setup-1.0.7-x64.exe installer.nsi

Unicode true
SetCompressor /SOLID lzma
SetCompressorDictSize 64

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"

!define PRODUCT "NOVA Workspace"
!define VERSION "1.0.7"
!define PUBLISHER "NOVA Workspace Team"
!define WEBSITE "https://nova-workspace.jayaswinjay.workers.dev"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\NOVAWorkspace"

!ifndef ARCH
  !define ARCH "x64"
!endif
!ifndef SRCDIR
  !error "SRCDIR is required (the folder holding novawrite.exe and friends)"
!endif
!ifndef ICONDIR
  !define ICONDIR "."
!endif
!ifndef HASDESK
  !define HASDESK 0
!endif

Name "${PRODUCT} ${VERSION} (${ARCH})"
OutFile "NOVAWorkspace-Setup-${ARCH}.exe"
InstallDir "$LOCALAPPDATA\Programs\NOVA Workspace"
InstallDirRegKey HKCU "Software\NOVAWorkspace" "InstallDir"
RequestExecutionLevel user
BrandingText "${PRODUCT} ${VERSION}"
ShowInstDetails show
ShowUninstDetails show

VIProductVersion "1.0.7.0"
VIAddVersionKey "ProductName" "${PRODUCT}"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "FileDescription" "${PRODUCT} installer (${ARCH})"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "CompanyName" "${PUBLISHER}"
VIAddVersionKey "LegalCopyright" "Copyright 2026 ${PUBLISHER}"
VIAddVersionKey "Comments" "${WEBSITE}"

!define MUI_ABORTWARNING
!define MUI_ICON "${ICONDIR}\novadesk.ico"
!define MUI_UNICON "${ICONDIR}\novadesk.ico"
!define MUI_WELCOMEPAGE_TITLE "Install ${PRODUCT} ${VERSION}"
!define MUI_WELCOMEPAGE_TEXT "This installs NOVA Write, NOVA Grid, NOVA Deck and NOVA PDF on this computer.$\r$\n$\r$\nIt is a per-user install: no administrator rights, no reboot, nothing added to the system outside your own account.$\r$\n$\r$\nThe suite is free for 30 days, then 6 rupees a month for one person. Nothing is collected and nothing is sent anywhere."

!define MUI_DIRECTORYPAGE_TEXT_TOP "NOVA Workspace will be installed in the folder below. Click Install to accept it, or choose another folder first."

!define MUI_FINISHPAGE_TITLE "${PRODUCT} is installed"
!define MUI_FINISHPAGE_TEXT "NOVA Write, Grid, Deck and PDF are on the Start menu.$\r$\n$\r$\nYour 30-day trial starts the first time you open NOVA. After that it is 6 rupees a month for one person."
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_TEXT "Open NOVA now"
!define MUI_FINISHPAGE_RUN_FUNCTION LaunchNova
!define MUI_FINISHPAGE_LINK "NOVA Workspace on the web"
!define MUI_FINISHPAGE_LINK_LOCATION "${WEBSITE}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

!macro ASSOC EXT PROGID
  WriteRegStr HKCU "Software\Classes\.${EXT}" "" "${PROGID}"
  WriteRegStr HKCU "Software\Classes\.${EXT}\OpenWithProgids" "${PROGID}" ""
!macroend

!macro UNASSOC EXT PROGID
  ReadRegStr $0 HKCU "Software\Classes\.${EXT}" ""
  StrCmp $0 "${PROGID}" 0 +2
    DeleteRegValue HKCU "Software\Classes\.${EXT}" ""
  DeleteRegValue HKCU "Software\Classes\.${EXT}\OpenWithProgids" "${PROGID}"
!macroend

Function LaunchNova
  !if ${HASDESK} == 1
    Exec '"$SYSDIR\wscript.exe" "$INSTDIR\Start NOVA Desk.vbs"'
  !else
    Exec '"$INSTDIR\novawrite.exe"'
  !endif
FunctionEnd

; ---------------------------------------------------------------------------
; Install
; ---------------------------------------------------------------------------

Section "!NOVA Write, Grid, Deck and PDF" SEC_CORE
  SectionIn RO

  SetOutPath "$INSTDIR"
  SetOverwrite on
  File /r "${SRCDIR}\*.*"

  SetOutPath "$INSTDIR\icons"
  File "${ICONDIR}\novawrite.ico"
  File "${ICONDIR}\novagrid.ico"
  File "${ICONDIR}\novadeck.ico"
  File "${ICONDIR}\novapdf.ico"
  !if ${HASDESK} == 1
    File "${ICONDIR}\novadesk.ico"
  !endif

  CreateDirectory "$SMPROGRAMS\NOVA Workspace"
  CreateShortcut "$SMPROGRAMS\NOVA Workspace\NOVA Write.lnk" "$INSTDIR\novawrite.exe" "" "$INSTDIR\icons\novawrite.ico" 0
  CreateShortcut "$SMPROGRAMS\NOVA Workspace\NOVA Grid.lnk" "$INSTDIR\novagrid.exe" "" "$INSTDIR\icons\novagrid.ico" 0
  CreateShortcut "$SMPROGRAMS\NOVA Workspace\NOVA Deck.lnk" "$INSTDIR\novadeck.exe" "" "$INSTDIR\icons\novadeck.ico" 0
  CreateShortcut "$SMPROGRAMS\NOVA Workspace\NOVA PDF.lnk" "$INSTDIR\novapdf.exe" "" "$INSTDIR\icons\novapdf.ico" 0
  !if ${HASDESK} == 1
    CreateShortcut "$SMPROGRAMS\NOVA Workspace\NOVA Desk.lnk" "$INSTDIR\Start NOVA Desk.vbs" "" "$INSTDIR\icons\novadesk.ico" 0
  !endif
  CreateShortcut "$SMPROGRAMS\NOVA Workspace\Read me.lnk" "$INSTDIR\README.txt"

  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateShortcut "$SMPROGRAMS\NOVA Workspace\Uninstall NOVA Workspace.lnk" "$INSTDIR\Uninstall.exe"

  WriteRegStr HKCU "${UNINST_KEY}" "DisplayName" "${PRODUCT} ${VERSION} (${ARCH})"
  WriteRegStr HKCU "${UNINST_KEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "${UNINST_KEY}" "Publisher" "${PUBLISHER}"
  WriteRegStr HKCU "${UNINST_KEY}" "URLInfoAbout" "${WEBSITE}"
  WriteRegStr HKCU "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINST_KEY}" "DisplayIcon" "$INSTDIR\icons\novadesk.ico"
  WriteRegStr HKCU "${UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKCU "${UNINST_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegDWORD HKCU "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINST_KEY}" "NoRepair" 1
  WriteRegDWORD HKCU "${UNINST_KEY}" "EstimatedSize" 250000

  WriteRegStr HKCU "Software\NOVAWorkspace" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\NOVAWorkspace" "Version" "${VERSION}"
SectionEnd

Section "Desktop shortcut" SEC_DESKTOP
  !if ${HASDESK} == 1
    CreateShortcut "$DESKTOP\NOVA Desk.lnk" "$INSTDIR\Start NOVA Desk.vbs" "" "$INSTDIR\icons\novadesk.ico" 0
  !else
    CreateShortcut "$DESKTOP\NOVA Write.lnk" "$INSTDIR\novawrite.exe" "" "$INSTDIR\icons\novawrite.ico" 0
  !endif
SectionEnd

Section "Open Office and PDF files with NOVA" SEC_ASSOC
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Write" "" "NOVA Write Document"
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Grid" "" "NOVA Grid Workbook"
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Deck" "" "NOVA Deck Presentation"
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Pdf" "" "NOVA PDF Document"

  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Write\DefaultIcon" "" "$INSTDIR\icons\novawrite.ico"
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Grid\DefaultIcon" "" "$INSTDIR\icons\novagrid.ico"
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Deck\DefaultIcon" "" "$INSTDIR\icons\novadeck.ico"
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Pdf\DefaultIcon" "" "$INSTDIR\icons\novapdf.ico"

  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Write\shell\open\command" "" '"$INSTDIR\novawrite.exe" "%1"'
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Grid\shell\open\command" "" '"$INSTDIR\novagrid.exe" "%1"'
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Deck\shell\open\command" "" '"$INSTDIR\novadeck.exe" "%1"'
  WriteRegStr HKCU "Software\Classes\NovaWorkspace.Pdf\shell\open\command" "" '"$INSTDIR\novapdf.exe" "%1"'

  !insertmacro ASSOC docx NovaWorkspace.Write
  !insertmacro ASSOC doc NovaWorkspace.Write
  !insertmacro ASSOC rtf NovaWorkspace.Write
  !insertmacro ASSOC txt NovaWorkspace.Write
  !insertmacro ASSOC odt NovaWorkspace.Write
  !insertmacro ASSOC md NovaWorkspace.Write
  !insertmacro ASSOC xlsx NovaWorkspace.Grid
  !insertmacro ASSOC xls NovaWorkspace.Grid
  !insertmacro ASSOC csv NovaWorkspace.Grid
  !insertmacro ASSOC tsv NovaWorkspace.Grid
  !insertmacro ASSOC novadeck NovaWorkspace.Deck
  !insertmacro ASSOC pptx NovaWorkspace.Deck
  !insertmacro ASSOC ppt NovaWorkspace.Deck
  !insertmacro ASSOC pdf NovaWorkspace.Pdf
SectionEnd

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_CORE} "NOVA Write, Grid, Deck and PDF, their licences and this installer's uninstaller. Always installed."
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_DESKTOP} "Put a shortcut on the desktop."
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_ASSOC} "Make .docx, .xlsx, .pptx, .pdf and the other supported types open in NOVA. Windows may still ask once which app to use."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; /NOASSOC and /NODESKTOP exist for unattended installs (and the build's own
; smoke test) that must not touch the current user's file types or desktop.
Function .onInit
  ${GetParameters} $R0
  ClearErrors
  ${GetOptions} $R0 "/NOASSOC" $R1
  ${IfNot} ${Errors}
    SectionSetFlags ${SEC_ASSOC} 0
  ${EndIf}
  ClearErrors
  ${GetOptions} $R0 "/NODESKTOP" $R1
  ${IfNot} ${Errors}
    SectionSetFlags ${SEC_DESKTOP} 0
  ${EndIf}
FunctionEnd

; ---------------------------------------------------------------------------
; Uninstall
; ---------------------------------------------------------------------------

Section "Uninstall"
  !insertmacro UNASSOC docx NovaWorkspace.Write
  !insertmacro UNASSOC doc NovaWorkspace.Write
  !insertmacro UNASSOC rtf NovaWorkspace.Write
  !insertmacro UNASSOC txt NovaWorkspace.Write
  !insertmacro UNASSOC odt NovaWorkspace.Write
  !insertmacro UNASSOC md NovaWorkspace.Write
  !insertmacro UNASSOC xlsx NovaWorkspace.Grid
  !insertmacro UNASSOC xls NovaWorkspace.Grid
  !insertmacro UNASSOC csv NovaWorkspace.Grid
  !insertmacro UNASSOC tsv NovaWorkspace.Grid
  !insertmacro UNASSOC novadeck NovaWorkspace.Deck
  !insertmacro UNASSOC pptx NovaWorkspace.Deck
  !insertmacro UNASSOC ppt NovaWorkspace.Deck
  !insertmacro UNASSOC pdf NovaWorkspace.Pdf

  DeleteRegKey HKCU "Software\Classes\NovaWorkspace.Write"
  DeleteRegKey HKCU "Software\Classes\NovaWorkspace.Grid"
  DeleteRegKey HKCU "Software\Classes\NovaWorkspace.Deck"
  DeleteRegKey HKCU "Software\Classes\NovaWorkspace.Pdf"
  DeleteRegKey HKCU "${UNINST_KEY}"
  DeleteRegKey HKCU "Software\NOVAWorkspace"

  Delete "$DESKTOP\NOVA Desk.lnk"
  Delete "$DESKTOP\NOVA Write.lnk"
  RMDir /r "$SMPROGRAMS\NOVA Workspace"

  RMDir /r "$INSTDIR"
SectionEnd
