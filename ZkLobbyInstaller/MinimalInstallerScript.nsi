#Author: xponen, for Zero-K, (from a modified Basic Example Script written by Joost Verburg)
#Date: 18 May 2015
#Function: Display code-of-conduct, download ZKL, and download & Install NET Framework if needed.
#Required Plugin: 
#    Stock plugin (preinstalled with NSIS)
#    UAC plugin (http://nsis.sourceforge.net/UAC_plug-in , REQUIRE NSIS version 2.46)

;--------------------------------
;Pre-Initialization stuff
#set best compression, ref: http://www.symantec.com/connect/articles/advanced-nsis-scripting-part-1
SetCompressor /FINAL /SOLID lzma
SetCompressorDictSize 64

# Request elevated privilege
!include "${NSISDIR}\UAC.nsh"
!include LogicLib.nsh

# Initialize
Var EST_ZKL_MAP_GAME_SPRING_SIZE_MB
Function .onInit
	ReserveFile ZeroKLobbyConfig.xml #add into Installer
	StrCpy $EST_ZKL_MAP_GAME_SPRING_SIZE_MB 600
	
	uac_tryagain:
	!insertmacro UAC_RunElevated
	${Switch} $0
	${Case} 0
		${IfThen} $1 = 1 ${|} Quit ${|} ;we are the outer process, the inner process has done its work, we are done
		${IfThen} $3 <> 0 ${|} ${Break} ${|} ;we are admin, let the show go on
		${If} $1 = 3 ;RunAs completed successfully, but with a non-admin user
			MessageBox MB_YESNO|MB_ICONEXCLAMATION|MB_TOPMOST|MB_SETFOREGROUND "Zero-K Lobby downloader requires admin privileges, try again" /SD IDNO IDYES uac_tryagain IDNO 0
		${EndIf}
		;fall-through and die
	${Case} 1223
		MessageBox MB_ICONSTOP|MB_TOPMOST|MB_SETFOREGROUND "Zero-K Lobby downloader requires admin privileges, aborting!"
		Quit
	${Case} 1062
		MessageBox MB_ICONSTOP|MB_TOPMOST|MB_SETFOREGROUND "Logon service not running, aborting!"
		Quit
	${Default}
		MessageBox MB_ICONSTOP|MB_TOPMOST|MB_SETFOREGROUND "Unable to elevate, error $0"
		Quit
	${EndSwitch}
	SetShellVarContext all
FunctionEnd

;--------------------------------
;User Interface stuff
;--------------------------------
;Include Modern UI
!include "MUI2.nsh"

;--------------------------------
;General
;Name and file
Name "Zero-K Lobby downloader"
OutFile "Zero-K Lobby Setup.exe"

;Default installation folder
InstallDir "$DOCUMENTS\My Games\Spring"

;Request application privileges
RequestExecutionLevel user #not needed (always set to user), because UAC will be controlled by UAC plugin

;--------------------------------
;Interface Settings

!define MUI_ICON "SpringDownloader.ico"
!define MUI_FINISHPAGE_NOAUTOCLOSE

;--------------------------------
;Pages
!define MUI_PAGE_HEADER_SUBTEXT "Please review the Zero-K community's code of conduct before continuing Zero-K Lobby Setup."
!define MUI_PAGE_HEADER_TEXT "Zero-K Community's Code of Conduct,"
!define MUI_LICENSEPAGE_CHECKBOX true
!define MUI_LICENSEPAGE_CHECKBOX_TEXT "I accept these code of conduct."
!define MUI_LICENSEPAGE_BUTTON "Next"
!define MUI_LICENSEPAGE_TEXT_TOP "Press Page Down to see the rest of the code of conduct."
!define MUI_LICENSEPAGE_TEXT_BOTTOM  "Please review the code of conduct before continuing Zero-K Lobby setup. If you accept the code of conduct, click the checkbox below. Click Next to continue."
!insertmacro MUI_PAGE_LICENSE "codeconduct.txt"

!define MUI_PAGE_HEADER_TEXT "Component"
!define MUI_PAGE_HEADER_SUBTEXT "Choose which features of Zero-K you want to download."
!define MUI_COMPONENTSPAGE_TEXT_TOP  "Map and Game is to be downloaded by Zero-K Lobby later. Click Next to continue.$\n(pre-installable media is being planned.)"
!insertmacro MUI_PAGE_COMPONENTS

!define MUI_PAGE_HEADER_SUBTEXT "Choose the folder in which you want to place Zero-K Lobby"
!define MUI_DIRECTORYPAGE_TEXT_TOP "Target folder should have at least 600MB disk space available for Zero-K Lobby to save minimum amount of Game, Spring engine, and Map files.  Click Install to continue."
!define MUI_PAGE_HEADER_TEXT "Directory"
!insertmacro MUI_PAGE_DIRECTORY

!define MUI_PAGE_HEADER_SUBTEXT "Please wait while Zero-K Lobby being downloaded"
!define MUI_PAGE_HEADER_TEXT  "Installation"
!define MUI_INSTFILESPAGE_FINISHHEADER_SUBTEXT  "Setup was completed successfully, click Next to continue."
!define MUI_INSTFILESPAGE_ABORTHEADER_SUBTEXT  "Setup was not completed successfully, click Cancel to exit."
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_TITLE "Complete Zero-K Lobby Setup"
!define MUI_FINISHPAGE_TEXT "Setup need to launch Zero-K Lobby to finish the setup.$\n$\nPlease launch Zero-K Lobby and wait for it to pop-up, to finish the rest of setup."
!define MUI_FINISHPAGE_BUTTON "Finish"
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_TEXT "Launch Zero-K Lobby now"
!define MUI_FINISHPAGE_RUN_FUNCTION runZeroKLobby
!insertmacro MUI_PAGE_FINISH
  
;--------------------------------
;Languages
 
  !insertmacro MUI_LANGUAGE "English"
;--------------------------------
;DirectioryPage stuff

!include "FileFunc.nsh"
Function verifyWriteable
	#require low-privilege to check for user-writable folder
	StrCpy $0 "0" #error flag (0|1)
	#1. check special folder
	ClearErrors
	${GetFileAttributes} "$INSTDIR" "SYSTEM" $0
	IfErrors test #folder didn't exist, try create new folder
		StrCmp "$0" "1" 0 write  #is system, abort
			Goto end
	write:
		#2. check file write
		FileOpen $0 "$INSTDIR\Test14124152.txt" w
		FileClose $0
		Delete "$INSTDIR\Test14124152.txt"
		IfErrors 0 end
			StrCpy $0 "1" # "Error: Target directory isn't writable!"
			Goto end
	test:
		#3. check folder creation
		ClearErrors
		CreateDirectory "$INSTDIR"
		RMDir "$INSTDIR"
		IfErrors 0 end
			StrCpy $0 "1" # "Error: Target directory not writable"
			Goto end
	end:
FunctionEnd

Function .onVerifyInstDir
	!insertmacro UAC_AsUser_Call Function verifyWriteable ${UAC_SYNCREGISTERS}|${UAC_SYNCINSTDIR}
	StrCmp "$0" "1" 0 end
		Abort
	end:
FunctionEnd

;--------------------------------
;Installer Sections

# Downloader section ref: http://nsis.sourceforge.net/How_to_Automatically_download_and_install_a_particular_version_of_.NET_if_it_is_not_already_installed
!include DetectNETFramework.nsh
!include nsDialogs.nsh
!include WinVer.nsh

Function DownloadAndInstallNet
		Abort
		${If} ${IsWinXP}
			Push 4
			Push 0
			Push 0
		${Else}
			Push 4
			Push 5
			Push 1
		${EndIf}
		Call FoundDotNETVersion
		Pop $0
		${If} $0 == 0
			${If} ${IsWinXP}
				MessageBox MB_OK|MB_ICONEXCLAMATION|MB_DEFBUTTON1 "NET Framework 4.0 isn't found in your system.$\n$\nSetup will install NET Framework 4.0"
				DetailPrint "Downloading: http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe"
				NSISdl::download /TIMEOUT=30000 http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe "$INSTDIR\dotNetFx40_Full_setup.exe"

			${Else}
				MessageBox MB_OK|MB_ICONEXCLAMATION|MB_DEFBUTTON1 "NET Framework 4.5.1 isn't found in your system.$\n$\nSetup will install NET Framework 4.5.1"
				DetailPrint "Downloading: http://download.microsoft.com/download/B/4/1/B4119C11-0423-477B-80EE-7A474314B347/NDP452-KB2901954-Web.exe"
				NSISdl::download /TIMEOUT=30000 http://download.microsoft.com/download/B/4/1/B4119C11-0423-477B-80EE-7A474314B347/NDP452-KB2901954-Web.exe "$INSTDIR\dotNetFx452-KB2901954-Web.exe"

			${EndIf}
			Pop $0 #from download stack
			StrCmp "$0" "success" +4
				DetailPrint "Download Failed: $0"
				MessageBox MB_OK|MB_ICONEXCLAMATION|MB_DEFBUTTON1 "NET Framework download failed$\n$\nError:$0"
				Abort
			
			${If} ${IsWinXP}
				#Banner download ref: http://nsis.sourceforge.net/How_to_Automatically_download_and_install_a_particular_version_of_.NET_if_it_is_not_already_installed
				Banner::show /NOUNLOAD "Waiting for NET4.0 Websetup ..."
				DetailPrint "Running $INSTDIR\dotNetFx40_Full_setup.exe"
				nsExec::ExecToStack "$INSTDIR\dotNetFx40_Full_setup.exe" /norestart
				Banner::destroy

			${Else}
				Banner::show /NOUNLOAD "Waiting for NET4.5.1 Websetup ..."
				DetailPrint "Running $INSTDIR\dotNetFx452-KB2901954-Web.exe"
				nsExec::ExecToStack "$INSTDIR\dotNetFx452-KB2901954-Web.exe" /norestart
				Push $0
				Banner::destroy

			${EndIf}
			#NET Framework installer exit code: https://msdn.microsoft.com/en-us/library/ee390831%28v=vs.110%29.aspx
			Pop $0 #from 
			StrCmp "$0" "0" 0 +3
				DetailPrint "NET Framework install succeed"
				Goto done
			StrCmp "$0" "1602" 0 +3
				DetailPrint "NET Framework install cancelled"
				Goto done
			StrCmp "$0" "1603" 0 +5
				DetailPrint "NET Framework install fatal error"
				MessageBox MB_OK|MB_ICONEXCLAMATION|MB_DEFBUTTON1 "NET Framework install fatal error"
				Abort
				Goto done
			StrCmp "$0" "1641" 0 +4
				DetailPrint "NET Framework install succeed. Require restart"
				SetRebootFlag true
				Goto done
			StrCmp "$0" "3010" 0 +4
				DetailPrint "NET Framework install succeed. Require restart"
				SetRebootFlag true
				Goto done
			StrCmp "$0" "5100" 0 +4
				DetailPrint "NET Framework failed to install: computer does not meet system requirements"
				MessageBox MB_OK|MB_ICONEXCLAMATION|MB_DEFBUTTON1 "NET Framework install failed: computer doesn't meet system requirements"
				Abort
			StrCmp "$0" "5101" 0 +4
				DetailPrint "NET Framework install internal error"
				MessageBox MB_OK|MB_ICONEXCLAMATION|MB_DEFBUTTON1 "NET Framework install internal error"
				Abort
			done:
		${EndIf}
FunctionEnd

Section "Zero-K Lobby" ZKL

	SetOutPath "$INSTDIR"
	
	#typically ZKL is 5.5Mb size
	AddSize 5500

	#check win version: http://nsis.sourceforge.net/Get_Windows_version
	#download file: http://nsis.sourceforge.net/NSISdl_plug-in
	${If} ${IsWinXP}
		DetailPrint "Downloading: http://zero-k.info/lobby/Zero-K_NET4.0.exe"
		NSISdl::download /TIMEOUT=30000 http://zero-k.info/lobby/Zero-K_NET4.0.exe "$INSTDIR\Zero-K_NET4.0.exe"
	${Else}
		DetailPrint "Downloading: http://zero-k.info/lobby/Zero-K.exe"
		NSISdl::download /TIMEOUT=30000 http://zero-k.info/lobby/Zero-K.exe "$INSTDIR\Zero-K.exe"
	${EndIf}
	Pop $0 #from download stack
	StrCmp "$0" "success" zklExist
		DetailPrint "Download Failed: $0"
		MessageBox MB_OK|MB_ICONEXCLAMATION|MB_DEFBUTTON1 "Zero-K Lobby download failed$\n$\nError:$0"
		Abort
	zklExist:
	Call DownloadAndInstallNet
	
	#Write basic config file
	SetOverwrite on
	File ZeroKLobbyConfig.xml
	
	#check diskspace
	DetailPrint "Checking disk space"
	StrCpy $1 $INSTDIR  3
	${DriveSpace} $1 "/D=F /S=M" $0 #Freespace in Megabyte
	IntOp $0 $0 - $EST_ZKL_MAP_GAME_SPRING_SIZE_MB
	${If} $0 <= 0
		MessageBox MB_OK|MB_ICONINFORMATION "Disk space might be too low for game data.$\n$\nIt is recommended to set a separate game data folder. Zero-K Lobby will prompt you for this."
		FileOpen $0 $INSTDIR\ZeroKLobbyConfig.xml a
		IfErrors end1 #unknown error
			FileSeek $0 -9 END #right before the word </Config>
			FileWrite $0 "  <DataFolder>invalid</DataFolder>$\r$\n"
			FileWrite $0 "</Config>"
			FileClose $0
		end1:
	${Else}
		FileOpen $0 $INSTDIR\ZeroKLobbyConfig.xml a
		IfErrors end2 #unknown error
			FileSeek $0 -9 END #right before the word </Config>
			FileWrite $0 "  <DataFolder>$INSTDIR</DataFolder>$\r$\n"
			FileWrite $0 "</Config>"
			FileClose $0
		end2:
	${EndIf}
SectionEnd
;--------------------------------
;Descriptions

	;Language strings
	LangString DESC_Zkl ${LANG_ENGLISH} "Zero-K Lobby is where you socialize with other players and launch multiplayer games."

	;Assign language strings to sections
	!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${Zkl} $(DESC_Zkl)
	!insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Post installation
Function runZeroKLobby
	${If} ${IsWinXP}
		!insertmacro UAC_AsUser_ExecShell "open" "$INSTDIR\Zero-K_NET4.0.exe" "" "" ""
	${Else}
		!insertmacro UAC_AsUser_ExecShell "open" "$INSTDIR\Zero-K.exe" "" "" ""
	${EndIf}
FunctionEnd

Function .onInstSuccess
	IfRebootFlag 0 end
		MessageBox MB_YESNO|MB_ICONQUESTION|MB_DEFBUTTON1 "NET Framework require reboot.$\nRestart Computer now?" IDNO end
		Reboot
	end:
FunctionEnd
