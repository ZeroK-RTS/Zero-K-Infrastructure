#Author: xponen, for Zero-K, (from a modified Basic Example Script written by Joost Verburg)
#Date: 18 May 2015
#Function: Display licence, download ZKL, and download & Install NET Framework if needed.

Var EST_ZKL_MAP_GAME_SPRING_SIZE_MB

Function .onInit
	IntOp $EST_ZKL_MAP_GAME_SPRING_SIZE_MB 600 + 0
FunctionEnd

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

;Request application privileges for Windows Vista
RequestExecutionLevel admin #for installing NET Framework

;--------------------------------
;Interface Settings

!define MUI_ICON "SpringDownloader.ico"
!define MUI_FINISHPAGE_NOAUTOCLOSE

;--------------------------------
;Pages
!define MUI_PAGE_HEADER_SUBTEXT "Please review the licences before downloading Zero-K Lobby"
!define MUI_PAGE_HEADER_TEXT "Licences"
!define MUI_LICENSEPAGE_BUTTON "OK"
!define MUI_LICENSEPAGE_TEXT_TOP "Scroll down to see the rest of the licence"
!define MUI_LICENSEPAGE_TEXT_BOTTOM  "$\nPress OK if you agree"
!insertmacro MUI_PAGE_LICENSE "legal.txt"

!define MUI_PAGE_HEADER_TEXT "Component"
!define MUI_PAGE_HEADER_SUBTEXT "Choose which features of Zero-K you want to download"
!define MUI_COMPONENTSPAGE_TEXT_TOP  "Map and Game will be downloaded by later by Zero-K Lobby. Press Next"
!insertmacro MUI_PAGE_COMPONENTS

!define MUI_PAGE_HEADER_SUBTEXT "Choose the folder in which you want to place Zero-K Lobby"
!define MUI_DIRECTORYPAGE_TEXT_TOP "No game data will be placed here yet. Press Install"
!define MUI_PAGE_HEADER_TEXT "Directory"
!insertmacro MUI_PAGE_DIRECTORY

!define MUI_PAGE_HEADER_SUBTEXT "Please wait while Zero-K Lobby being downloaded"
!define MUI_PAGE_HEADER_TEXT  "Installation"
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_TITLE "Complete Zero-K Lobby Setup"
!define MUI_FINISHPAGE_TEXT "Press Launch, and wait for Zero-K Lobby to appear to finish setup..."
!define MUI_FINISHPAGE_BUTTON "Launch"
!insertmacro MUI_PAGE_FINISH
  
;--------------------------------
;Languages
 
  !insertmacro MUI_LANGUAGE "English"
;--------------------------------
;DirectioryPage stuff

Function .onVerifyInstDir
	!include "FileFunc.nsh"
	
	#check writability
	ClearErrors
	FileOpen $0 "$INSTDIR\Test14124152.txt" w
	FileClose $0
	Delete "$INSTDIR\Test14124152.txt"
	IfErrors 0 end
		Abort # "Error: Target directory isn't writable!"
	end:
FunctionEnd

;--------------------------------
;Installer Sections

# Downloader section ref: http://nsis.sourceforge.net/How_to_Automatically_download_and_install_a_particular_version_of_.NET_if_it_is_not_already_installed
!include DetectNETFramework.nsh
!include nsDialogs.nsh
!include WinVer.nsh

Function DownloadAndInstallNet
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
		${IfNot} $0 == 1
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
	
	#check diskspace
	StrCpy $1 $INSTDIR  3
	${DriveSpace} $1 "/D=F /S=M" $0 #Freespace in Megabyte
	IntOp $0 $0 - $EST_ZKL_MAP_GAME_SPRING_SIZE_MB
	${If} $0 <= 0
		MessageBox MB_OK|MB_ICONINFORMATION "Disk space might be too low for game data.$\nPlease configure ZKL for a different data directory later."
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
Function .onInstSuccess
	IfRebootFlag 0 continue
		MessageBox MB_YESNO|MB_ICONQUESTION|MB_DEFBUTTON1 "NET Framework require reboot.$\nRestart Computer now?" IDNO continue
		Reboot
		Goto end
	continue:
	${If} ${IsWinXP}
		ExecShell "open" "$INSTDIR\Zero-K_NET4.0.exe"
	${Else}
		ExecShell "open" "$INSTDIR\Zero-K.exe"
	${EndIf}
	end:
FunctionEnd
