# Auto-generated by EclipseNSIS Script Wizard
# Apr 26, 2009 9:49:49 AM

Name "WBFS Manager 3.0"

RequestExecutionLevel admin

# General Symbol Definitions
!define REGKEY "SOFTWARE\$(^Name)"
!define VERSION 3.0
!define COMPANY AlexDP
!define URL wbfsmanager.codeplex.com
!define SF_USELECTED 0

# MUI Symbol Definitions
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_LICENSEPAGE_CHECKBOX
!define MUI_STARTMENUPAGE_REGISTRY_ROOT HKLM
!define MUI_STARTMENUPAGE_REGISTRY_KEY ${REGKEY}
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME StartMenuGroup
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "WBFS Manager"
!define MUI_FINISHPAGE_SHOWREADME $INSTDIR\Readme.txt
!define MUI_UNICON ..\x86\WBFSManager.ico
!define MUI_UNFINISHPAGE_NOAUTOCLOSE

# For checking if program is open before uninstalling
!define WNDTITLE "WBFS Manager 3.0"

# For checking if .NET FW 3.5 is installed
!define DOT_MAJOR 3
!define DOT_MINOR 5

# Included files
!include Sections.nsh
!include MUI2.nsh

# Variables
Var StartMenuGroup

# Installer pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE ..\EULA.txt
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_STARTMENU Application $StartMenuGroup
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

# Installer languages
!insertmacro MUI_LANGUAGE English

#### .NET FW 3.5 SP1 Checks
!macro SecSelect SecId
  Push $0
  IntOp $0 ${SF_SELECTED} | ${SF_RO}
  SectionSetFlags ${SecId} $0
  SectionSetInstTypes ${SecId} 1
  Pop $0
!macroend
 
!define SelectSection '!insertmacro SecSelect'
#################################
 
!macro SecUnSelect SecId
  Push $0
  IntOp $0 ${SF_USELECTED} | ${SF_RO}
  SectionSetFlags ${SecId} $0
  SectionSetText  ${SecId} ""
  Pop $0
!macroend
 
!define UnSelectSection '!insertmacro SecUnSelect'
###################################
 
!macro SecExtract SecId
  Push $0
  IntOp $0 ${SF_USELECTED} | ${SF_RO}
  SectionSetFlags ${SecId} $0
  SectionSetInstTypes ${SecId} 2
  Pop $0
!macroend
 
!define SetSectionExtract '!insertmacro SecExtract'
###################################
 
!macro Groups GroupId
  Push $0
  SectionGetFlags ${GroupId} $0
  IntOp $0 $0 | ${SF_RO}
  IntOp $0 $0 ^ ${SF_BOLD}
  IntOp $0 $0 ^ ${SF_EXPAND}
  SectionSetFlags ${GroupId} $0
  Pop $0
!macroend
 
!define SetSectionGroup "!insertmacro Groups"
####################################
 
!macro GroupRO GroupId
  Push $0
  IntOp $0 ${SF_SECGRP} | ${SF_RO}
  SectionSetFlags ${GroupId} $0
  Pop $0
!macroend
 
!define MakeGroupReadOnly '!insertmacro GroupRO'
###################################################### End .NET 3.5 SP1

# Installer attributes
OutFile setup.exe
InstallDir "$PROGRAMFILES\WBFS\WBFS Manager 3.0"
CRCCheck on
XPStyle on
ShowInstDetails show
VIProductVersion 3.0.0.0
VIAddVersionKey ProductName "WBFS Manager 3.0"
VIAddVersionKey ProductVersion "${VERSION}"
VIAddVersionKey CompanyName "${COMPANY}"
VIAddVersionKey CompanyWebsite "${URL}"
VIAddVersionKey FileVersion "${VERSION}"
VIAddVersionKey FileDescription ""
VIAddVersionKey LegalCopyright ""
InstallDirRegKey HKLM "${REGKEY}" Path
ShowUninstDetails show


#############   .NET3.5 SP1
!define BASE_URL http://download.microsoft.com/download
; .NET Framework
; Same for all http://download.microsoft.com/download/0/6/1/061F001C-8752-4600-A198-53214C69B51F/dotnetfx35setup.exe
!define URL_DOTNET "${BASE_URL}/0/6/1/061F001C-8752-4600-A198-53214C69B51F/dotnetfx35setup.exe"

Var "DOTNET_RETURN_CODE"

LangString DESC_REMAINING ${LANG_ENGLISH} " (%d %s%s remaining)"
LangString DESC_PROGRESS ${LANG_ENGLISH} "%d.%01dkB/s" ;"%dkB (%d%%) of %dkB @ %d.%01dkB/s"
LangString DESC_PLURAL ${LANG_ENGLISH} "s"
LangString DESC_HOUR ${LANG_ENGLISH} "hour"
LangString DESC_MINUTE ${LANG_ENGLISH} "minute"
LangString DESC_SECOND ${LANG_ENGLISH} "second"
LangString DESC_CONNECTING ${LANG_ENGLISH} "Connecting..."
LangString DESC_DOWNLOADING ${LANG_ENGLISH} "Downloading %s"
LangString DESC_SHORTDOTNET ${LANG_ENGLISH} "Microsoft .NET Framework 3.5 SP1"
LangString DESC_LONGDOTNET ${LANG_ENGLISH} "Microsoft .NET Framework 3.5 SP1"
LangString DESC_DOTNET_DECISION ${LANG_ENGLISH} "$(DESC_SHORTDOTNET) is required.$\nIt is strongly \
  advised that you install$\n$(DESC_SHORTDOTNET) before continuing.$\nIf you choose to continue, \
  you will need to connect$\nto the internet before proceeding.$\nWould you like to continue with \
  the installation?"
LangString SEC_DOTNET ${LANG_ENGLISH} "$(DESC_SHORTDOTNET) "
LangString DESC_INSTALLING ${LANG_ENGLISH} "Installing"
LangString DESC_DOWNLOADING1 ${LANG_ENGLISH} "Downloading"
LangString DESC_DOWNLOADFAILED ${LANG_ENGLISH} "Download Failed:"
LangString ERROR_DOTNET_DUPLICATE_INSTANCE ${LANG_ENGLISH} "The $(DESC_SHORTDOTNET) Installer is \
  already running."
LangString ERROR_NOT_ADMINISTRATOR ${LANG_ENGLISH} "This installation requires Administrator privileges."
LangString ERROR_INVALID_PLATFORM ${LANG_ENGLISH} "Incorrect platform for this installation."
LangString DESC_DOTNET_TIMEOUT ${LANG_ENGLISH} "The installation of the $(DESC_SHORTDOTNET) \
  has timed out."
LangString ERROR_DOTNET_INVALID_PATH ${LANG_ENGLISH} "The $(DESC_SHORTDOTNET) Installation$\n\
  was not found in the following location:$\n"
LangString ERROR_DOTNET_FATAL ${LANG_ENGLISH} "A fatal error occurred during the installation$\n\
  of the $(DESC_SHORTDOTNET)."
LangString FAILED_DOTNET_INSTALL ${LANG_ENGLISH} "The installation of $(PRODUCTNAME) will$\n\
  continue. However, it may not function properly$\nuntil $(DESC_SHORTDOTNET)$\nis installed."
LangString NOTICE_REBOOT_REQUIRED ${LANG_ENGLISH} "A reboot may be necessary for $(PRODUCTNAME) to function \
    correctly, due to the installation of .NET Framework 3.5 SP1."

Section $(SEC_DOTNET) SECDOTNET
    SectionIn RO
    ;IfSilent lbl_IsSilent
    !define DOTNETFILESDIR "Common\Files\MSNET"
    StrCpy $DOTNET_RETURN_CODE "0"
 
    ; the following Goto and Label is for consistencey.
    Goto lbl_DownloadRequired
    lbl_DownloadRequired:
    DetailPrint "$(DESC_DOWNLOADING1) $(DESC_SHORTDOTNET)..."
    MessageBox MB_ICONEXCLAMATION|MB_YESNO|MB_DEFBUTTON2 "$(DESC_DOTNET_DECISION)" /SD IDNO \
      IDYES +2 IDNO 0
    Abort "Setup failed."
    ; "Downloading Microsoft .Net Framework"
    AddSize 2961
    nsisdl::download /TRANSLATE "$(DESC_DOWNLOADING)" "$(DESC_CONNECTING)" \
       "$(DESC_SECOND)" "$(DESC_MINUTE)" "$(DESC_HOUR)" "$(DESC_PLURAL)" \
       "$(DESC_PROGRESS)" "$(DESC_REMAINING)" \
       /TIMEOUT=300000 "${URL_DOTNET}" "$PLUGINSDIR\dotnetfx.exe"
    Pop $0
    StrCmp "$0" "success" lbl_continue
    DetailPrint "$(DESC_DOWNLOADFAILED) $0"
    Abort "Setup failed."
 
    lbl_continue:
      DetailPrint "$(DESC_INSTALLING) $(DESC_SHORTDOTNET)..."
      Banner::show /NOUNLOAD "$(DESC_INSTALLING) $(DESC_SHORTDOTNET)..."
      nsExec::ExecToStack '"$PLUGINSDIR\dotnetfx.exe" /qb /norestart'
      pop $DOTNET_RETURN_CODE
      Banner::destroy
      ; silence the compiler
      Goto lbl_NoDownloadRequired
      lbl_NoDownloadRequired:
 
      ; obtain any error code and inform the user ($DOTNET_RETURN_CODE)
      ; If nsExec is unable to execute the process,
      ; it will return "error"
      ; If the process timed out it will return "timeout"
      ; else it will return the return code from the executed process.
      StrCmp "$DOTNET_RETURN_CODE" "" lbl_NoError
      StrCmp "$DOTNET_RETURN_CODE" "0" lbl_NoError
      StrCmp "$DOTNET_RETURN_CODE" "3010" lbl_RebootReq
      StrCmp "$DOTNET_RETURN_CODE" "8192" lbl_NoError
      StrCmp "$DOTNET_RETURN_CODE" "error" lbl_Error
      StrCmp "$DOTNET_RETURN_CODE" "timeout" lbl_TimeOut
      ; It's a .Net Error
      StrCmp "$DOTNET_RETURN_CODE" "4101" lbl_Error_DuplicateInstance
      StrCmp "$DOTNET_RETURN_CODE" "4097" lbl_Error_NotAdministrator
      StrCmp "$DOTNET_RETURN_CODE" "1633" lbl_Error_InvalidPlatform
      StrCmp "$DOTNET_RETURN_CODE" "1602" lbl_UserExit
      StrCmp "$DOTNET_RETURN_CODE" "1603" lbl_FatalError
      StrCmp "$DOTNET_RETURN_CODE" "1605" lbl_Unkown_product
      StrCmp "$DOTNET_RETURN_CODE" "1636" lbl_Invalid_patch
      StrCmp "$DOTNET_RETURN_CODE" "1639" lbl_Invalid_cmdLine
      StrCmp "$DOTNET_RETURN_CODE" "1643" lbl_SysPolicy lbl_FatalError
      ; all others are fatal
 
    lbl_Error_DuplicateInstance:
    DetailPrint "$(ERROR_DOTNET_DUPLICATE_INSTANCE)"
    GoTo lbl_Done
 
    lbl_Error_NotAdministrator:
    DetailPrint "$(ERROR_NOT_ADMINISTRATOR)"
    GoTo lbl_Done
 
    lbl_Error_InvalidPlatform:
    DetailPrint "$(ERROR_INVALID_PLATFORM)"
    GoTo lbl_Done
 
    lbl_TimeOut:
    DetailPrint "$(DESC_DOTNET_TIMEOUT)"
    GoTo lbl_Done
 
    lbl_Error:
    DetailPrint "$(ERROR_DOTNET_INVALID_PATH)"
    GoTo lbl_Done
 
    lbl_FatalError:
    lbl_Unkown_product:
    lbl_Invalid_patch:
    lbl_Invalid_cmdLine:
    lbl_SysPolicy:
    DetailPrint "$(ERROR_DOTNET_FATAL)[$DOTNET_RETURN_CODE]"
    GoTo lbl_Done
    
    lbl_RebootReq:
    SetRebootFlag true
    DetailPrint "$(NOTICE_REBOOT_REQUIRED)"
    GoTo lbl_NoError
    
    lbl_UserExit:    
    lbl_Done:
    DetailPrint "$(FAILED_DOTNET_INSTALL)"
    Abort "Setup failed."
    lbl_NoError:
    
SectionEnd
####   END .NET 3.5SP1


# Installer sections
Section "-Required Application files" SEC0000
    SetOutPath $INSTDIR
    SetOverwrite on
    File ..\x86\WBFSManager.exe
    File ..\x86\bannerExtractionWrapper.dll
    File ..\x86\ChannelCreation.dll
    File ..\x86\ChannelCreationWrapper.dll
    File ..\x86\Infralution.Localization.Wpf.dll
    File ..\x86\libeay32.dll
    File ..\x86\libwbfsNET.dll
    File ..\x86\libwbfsNETwrapper.dll
    File ..\x86\RssUpdater.dll
    File ..\x86\unrar.dll
    File ..\x86\UnRarNET.dll
    File ..\x86\WBFSManager.ico
    File ..\x86\Readme.txt
    File ..\x86\WBFSManager.exe.config
    SetOutPath $INSTDIR\en
    SetOverwrite on
    File ..\x86\en\*
    SetOutPath $INSTDIR\HBC
    File /r ..\x86\HBC\*
    CreateDirectory $INSTDIR\Channels
    CreateDirectory "$DOCUMENTS\WBFS Manager Covers"
    SetOutPath "$DOCUMENTS\WBFS Manager Covers"
    File "..\x86\WBFS Manager Covers\Readme.txt"
    SetOutPath $WINDIR
    SetOverwrite off
    File /r ..\x86\Windows\*
    SetOverwrite on
    WriteRegStr HKLM "${REGKEY}\Components" "Required Application files" 1
SectionEnd

Section "Channel Creation Support" SEC0001
    SetOutPath $INSTDIR\Channels
    SetOverwrite on
    File /nonfatal /r ..\x86\Channels\*
    WriteRegStr HKLM "${REGKEY}\Components" "Channel Creation Support" 1
SectionEnd

SectionGroup "Multi-Lingual Support" SECGRP0000
    Section Italiano SEC0002
        SetOutPath $INSTDIR\it
        SetOverwrite on
        File /nonfatal /r ..\x86\it\*
        WriteRegStr HKLM "${REGKEY}\Components" Italiano 1
    SectionEnd

    Section French SEC0003
        SetOutPath $INSTDIR\fr
        SetOverwrite on
        File /nonfatal /r ..\x86\fr\*
        WriteRegStr HKLM "${REGKEY}\Components" French 1
    SectionEnd

    Section Deutsch SEC0004
        SetOutPath $INSTDIR\de
        SetOverwrite on
        File /nonfatal /r ..\x86\de\*
        WriteRegStr HKLM "${REGKEY}\Components" Deutsch 1
    SectionEnd

    Section Espa�ol SEC0005
        SetOutPath $INSTDIR\es
        SetOverwrite on
        File /nonfatal /r ..\x86\es\*
        WriteRegStr HKLM "${REGKEY}\Components" Espa�ol 1
    SectionEnd

    Section Nederlands SEC0006
        SetOutPath $INSTDIR\nl
        SetOverwrite on
        File /nonfatal /r ..\x86\nl\*
        WriteRegStr HKLM "${REGKEY}\Components" Nederlands 1
    SectionEnd

    Section Donca SEC0007
        SetOutPath $INSTDIR\it-CH
        SetOverwrite on
        File /nonfatal /r ..\x86\it-CH\*
        WriteRegStr HKLM "${REGKEY}\Components" Donca 1
    SectionEnd

    Section "Chinese (Traditional)" SEC0008
        SetOutPath $INSTDIR\zh-CHT
        SetOverwrite on
        File /nonfatal /r ..\x86\zh-CHT\*
        WriteRegStr HKLM "${REGKEY}\Components" "Chinese (Traditional)" 1
    SectionEnd
SectionGroupEnd

!macro CREATE_SMGROUP_SHORTCUT NAME PATH
    Push "${NAME}"
    Push "${PATH}"
    Call CreateSMGroupShortcut
!macroend

SectionGroup Shortcuts SECGRP0001
    Section "Desktop Shortcuts" SEC0009
        SetOutPath $DESKTOP
        CreateShortcut "$DESKTOP\WBFS Manager 3.0.lnk" $INSTDIR\WBFSManager.exe
        WriteRegStr HKLM "${REGKEY}\Components" "Desktop Shortcuts" 1
    SectionEnd

    Section "Start Menu" SEC0010
        !insertmacro CREATE_SMGROUP_SHORTCUT "WBFS Manager 3.0" $INSTDIR\WBFSManager.exe
        WriteRegStr HKLM "${REGKEY}\Components" "Start Menu" 1
    SectionEnd
SectionGroupEnd

Section -post SEC0011
    WriteRegStr HKLM "${REGKEY}" Path $INSTDIR
    SetOutPath $INSTDIR
    WriteUninstaller $INSTDIR\uninstall.exe
    !insertmacro MUI_STARTMENU_WRITE_BEGIN Application
    !insertmacro MUI_STARTMENU_WRITE_END
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" DisplayName "$(^Name)"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" DisplayVersion "${VERSION}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" Publisher "${COMPANY}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" URLInfoAbout "${URL}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" DisplayIcon $INSTDIR\uninstall.exe
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" UninstallString $INSTDIR\uninstall.exe
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" NoModify 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" NoRepair 1
SectionEnd

# Macro for selecting uninstaller sections
!macro SELECT_UNSECTION SECTION_NAME UNSECTION_ID
    Push $R0
    ReadRegStr $R0 HKLM "${REGKEY}\Components" "${SECTION_NAME}"
    StrCmp $R0 1 0 next${UNSECTION_ID}
    !insertmacro SelectSection "${UNSECTION_ID}"
    GoTo done${UNSECTION_ID}
next${UNSECTION_ID}:
    !insertmacro UnselectSection "${UNSECTION_ID}"
done${UNSECTION_ID}:
    Pop $R0
!macroend

# Uninstaller sections
!macro DELETE_SMGROUP_SHORTCUT NAME
    Push "${NAME}"
    Call un.DeleteSMGroupShortcut
!macroend

Section /o "-un.Start Menu" UNSEC0010
    !insertmacro DELETE_SMGROUP_SHORTCUT "WBFS Manager 3.0"
    DeleteRegValue HKLM "${REGKEY}\Components" "Start Menu"
SectionEnd

Section /o "-un.Desktop Shortcuts" UNSEC0009
    Delete /REBOOTOK "$DESKTOP\WBFS Manager 3.0.lnk"
    DeleteRegValue HKLM "${REGKEY}\Components" "Desktop Shortcuts"
SectionEnd

Section /o "-un.Chinese (Traditional)" UNSEC0008
    RmDir /r /REBOOTOK $INSTDIR\zh-CHT
    DeleteRegValue HKLM "${REGKEY}\Components" "Chinese (Traditional)"
SectionEnd

Section /o -un.Donca UNSEC0007
    RmDir /r /REBOOTOK $INSTDIR\it-CH
    DeleteRegValue HKLM "${REGKEY}\Components" Donca
SectionEnd

Section /o -un.Nederlands UNSEC0006
    RmDir /r /REBOOTOK $INSTDIR\nl
    DeleteRegValue HKLM "${REGKEY}\Components" Nederlands
SectionEnd

Section /o -un.Espa�ol UNSEC0005
    RmDir /r /REBOOTOK $INSTDIR\es
    DeleteRegValue HKLM "${REGKEY}\Components" Espa�ol
SectionEnd

Section /o -un.Deutsch UNSEC0004
    RmDir /r /REBOOTOK $INSTDIR\de
    DeleteRegValue HKLM "${REGKEY}\Components" Deutsch
SectionEnd

Section /o -un.French UNSEC0003
    RmDir /r /REBOOTOK $INSTDIR\fr
    DeleteRegValue HKLM "${REGKEY}\Components" French
SectionEnd

Section /o -un.Italiano UNSEC0002
    RmDir /r /REBOOTOK $INSTDIR\it
    DeleteRegValue HKLM "${REGKEY}\Components" Italiano
SectionEnd

Section /o "-un.Channel Creation Support" UNSEC0001
    RmDir /r /REBOOTOK $INSTDIR\Channels
    DeleteRegValue HKLM "${REGKEY}\Components" "Channel Creation Support"
SectionEnd

Section /o "-un.Required Application files" UNSEC0000
    Delete /REBOOTOK $INSTDIR\WBFSManager.ico
    Delete /REBOOTOK $INSTDIR\WBFSManager.exe.config
    Delete /REBOOTOK $INSTDIR\WBFSManager.exe
    Delete /REBOOTOK $INSTDIR\UnRarNET.dll
    Delete /REBOOTOK $INSTDIR\unrar.dll
    Delete /REBOOTOK $INSTDIR\RssUpdater.dll
    Delete /REBOOTOK $INSTDIR\Readme.txt
    Delete /REBOOTOK $INSTDIR\libwbfsNETwrapper.dll
    Delete /REBOOTOK $INSTDIR\libwbfsNET.dll
    Delete /REBOOTOK $INSTDIR\LIBEAY32.dll
    Delete /REBOOTOK $INSTDIR\Infralution.Localization.Wpf.dll
    Delete /REBOOTOK $INSTDIR\ChannelCreationWrapper.dll
    Delete /REBOOTOK $INSTDIR\ChannelCreation.dll
    Delete /REBOOTOK $INSTDIR\bannerExtractionWrapper.dll
    RmDir /r /REBOOTOK $INSTDIR\en
    RmDir /r /REBOOTOK $INSTDIR\HBC
    RmDir /r /REBOOTOK $INSTDIR\Channels
    DeleteRegValue HKLM "${REGKEY}\Components" "Required Application files"
SectionEnd

Section -un.post UNSEC0011
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)"
    Delete /REBOOTOK $INSTDIR\uninstall.exe
    DeleteRegValue HKLM "${REGKEY}" StartMenuGroup
    DeleteRegValue HKLM "${REGKEY}" Path
    DeleteRegKey /IfEmpty HKLM "${REGKEY}\Components"
    DeleteRegKey /IfEmpty HKLM "${REGKEY}"
    RmDir /REBOOTOK $SMPROGRAMS\$StartMenuGroup
    RmDir /REBOOTOK $INSTDIR
    Push $R0
    StrCpy $R0 $StartMenuGroup 1
    StrCmp $R0 ">" no_smgroup
no_smgroup:
    Pop $R0
SectionEnd



# Installer functions
Function .onInit
    InitPluginsDir
    SetOutPath "$PLUGINSDIR"
    File /r "${NSISDIR}\Plugins\*.*"
    Call SetupDotNetSectionIfNeeded 
    Call BitnessCheck
FunctionEnd

Function CreateSMGroupShortcut
    Exch $R0 ;PATH
    Exch
    Exch $R1 ;NAME
    Push $R2
    StrCpy $R2 $StartMenuGroup 1
    StrCmp $R2 ">" no_smgroup
    SetOutPath $SMPROGRAMS\$StartMenuGroup
    CreateShortcut "$SMPROGRAMS\$StartMenuGroup\$R1.lnk" $R0
no_smgroup:
    Pop $R2
    Pop $R1
    Pop $R0
FunctionEnd

# Uninstaller functions
Function un.onInit
    ReadRegStr $INSTDIR HKLM "${REGKEY}" Path
    !insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuGroup
    !insertmacro SELECT_UNSECTION "Required Application files" ${UNSEC0000}
    !insertmacro SELECT_UNSECTION "Channel Creation Support" ${UNSEC0001}
    !insertmacro SELECT_UNSECTION Italiano ${UNSEC0002}
    !insertmacro SELECT_UNSECTION French ${UNSEC0003}
    !insertmacro SELECT_UNSECTION Deutsch ${UNSEC0004}
    !insertmacro SELECT_UNSECTION Espa�ol ${UNSEC0005}
    !insertmacro SELECT_UNSECTION Nederlands ${UNSEC0006}
    !insertmacro SELECT_UNSECTION Donca ${UNSEC0007}
    !insertmacro SELECT_UNSECTION "Chinese (Traditional)" ${UNSEC0008}
    !insertmacro SELECT_UNSECTION "Desktop Shortcuts" ${UNSEC0009}
    !insertmacro SELECT_UNSECTION "Start Menu" ${UNSEC0010}
    FindWindow $0 "" "${WNDTITLE}"
    StrCmp $0 0 continueInstall
        MessageBox MB_ICONSTOP|MB_OK "The application you are trying to remove is running. Close it and try again."
        Abort  "Uninstall failed."
   continueInstall:
FunctionEnd

Function un.DeleteSMGroupShortcut
    Exch $R1 ;NAME
    Push $R2
    StrCpy $R2 $StartMenuGroup 1
    StrCmp $R2 ">" no_smgroup
    Delete /REBOOTOK "$SMPROGRAMS\$StartMenuGroup\$R1.lnk"
no_smgroup:
    Pop $R2
    Pop $R1
FunctionEnd

Function BitnessCheck
    GetVersion::WindowsPlatformArchitecture
    Pop $R0
    StrCmp $R0 "32" is32Bit is64Bit
is64Bit:
    MessageBox MB_ICONSTOP|MB_OK "This installer is for the 64-bit version of $(PRODUCTNAME), you are running a 32-bit operating system. \
        Please download and install the 32-bit version of $(PRODUCTNAME)."
    Abort "Setup failed."
is32Bit:
FunctionEnd

###########            .NET 3.5 SP1 stuff
Function SetupDotNetSectionIfNeeded
 
  StrCpy $0 "0"
  StrCpy $1 "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" ;registry entry to look in.
  StrCpy $2 0
 
  ReadRegDWORD $3 HKLM "$1" "SP"
  ;If we don't find any versions installed, it's not here.
  StrCmp $3 "" noDotNet notEmpty
  
  ;We found something.
  notEmpty:
      IntCmp $3 1 yesDotNet noDotNet yesDotNet ;equal, less than, greater than
      
 
  noDotNet:
    ${SelectSection} ${SECDOTNET}
    goto done
 
  yesDotNet:
    ;Everything checks out.  Go on with the rest of the installation.
    ${UnSelectSection} ${SECDOTNET}
    goto done
 
  done:
    ;All done.
 
FunctionEnd


 
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
!insertmacro MUI_DESCRIPTION_TEXT ${SECDOTNET} $(DESC_LONGDOTNET)



#####                END .NET FW 3.5 SP1 stuff


# Section Descriptions
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0001} $(SEC0001_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SECGRP0000} $(SECGRP0000_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0002} $(SEC0002_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0003} $(SEC0003_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0004} $(SEC0004_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0005} $(SEC0005_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0006} $(SEC0006_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0007} $(SEC0007_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0008} $(SEC0008_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SECGRP0001} $(SECGRP0001_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0009} $(SEC0009_DESC)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC0010} $(SEC0010_DESC)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

# Installer Language Strings
# TODO Update the Language Strings with the appropriate translations.

LangString SEC0001_DESC ${LANG_ENGLISH} "Choose this to install USB Loader dol files and other packaged channel creation files."
LangString SECGRP0000_DESC ${LANG_ENGLISH} "Choose from different languages to install support for."
LangString SEC0002_DESC ${LANG_ENGLISH} "Italian language support."
LangString SEC0003_DESC ${LANG_ENGLISH} "French language support."
LangString SEC0004_DESC ${LANG_ENGLISH} "German langauge support."
LangString SEC0005_DESC ${LANG_ENGLISH} "Spanish language support."
LangString SEC0006_DESC ${LANG_ENGLISH} "Dutch language support."
LangString SEC0007_DESC ${LANG_ENGLISH} "Perugino language support."
LangString SEC0008_DESC ${LANG_ENGLISH} "Chinese (Traditional) language support."
LangString SECGRP0001_DESC ${LANG_ENGLISH} "Choose which shortcuts you want."
LangString SEC0009_DESC ${LANG_ENGLISH} "Add a shortcut to WBFS Manager to your Desktop."
LangString SEC0010_DESC ${LANG_ENGLISH} "Add a shortcut to WBFS Manager to your Start Menu."
LangString PRODUCTNAME ${LANG_ENGLISH} "WBFS Manager 3.0"