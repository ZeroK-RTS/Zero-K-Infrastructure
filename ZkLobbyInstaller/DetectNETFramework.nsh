#Author: xponen
#Date: 17 May 2015
#Function: Detect whether the stated NET version is installed or not.
#Reference: How to check NET Framework: https://msdn.microsoft.com/en-us/library/hh925568%28v=vs.110%29.aspx?f=255&MSPPError=-21472173961

Function FoundDotNETVersion
	Pop $5  #subVersion
	Pop $4  #minorVersion
	Pop $3  #majorVersion
	
	#Register manipulated in this function
	#$0 as index
	#$1 as registry Key (net1-4 segment) or Value (net4.5 segment)
	#$2 as registry Data
	
	IntCmp $3 4 equal less fourFive
	equal:
		IntCmp $4 5 fourFive less fourFive
	
	#checking for NET 1.0 to NET 4.0
	less:
		${If} $3 == 4
			StrCpy $2 "v4"
		${ElseIf} $5 == 0
			#eg v1.0, v1.1, v2.0, v3.0, v3.5
			StrCpy $2 "v$3.$4"
		${Else}
			#eg v2.0.50727
			StrCpy $2 "v$3.$4.$5"
		${EndIf}
		EnumRegKey $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\$2" $0
		IfErrors doneFail
		Goto doneSucceed
	#end less

	#checking for NET 4.5+
	fourFive:
		StrCpy $0 0
		loop:
			EnumRegValue  $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" $0
			IfErrors doneFail
			IntOp $0 $0 + 1 #index++
			${IfNot} $1 == "Release"
				Goto loop
			${EndIf}
			ReadRegStr $2 HKLM  "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" $1
			#.NET Framework 4.5
			${If} $2 == 378389
				${If} $4 == 5
					${If} $5 == 0
						Goto doneSucceed
					${EndIf}
				${EndIf}
				Goto doneFail
			${EndIf}
			#.NET Framework 4.5.1 installed with Windows 8.1
			${If} $2 == 378675
				${If} $4 == 5
					${If} $5 <= 1
						Goto doneSucceed
					${EndIf}
				${EndIf}
				Goto doneFail
			${EndIf}
			#.NET Framework 4.5.1 installed on Windows 8, Windows 7 SP1, or Windows Vista SP2
			${If} $2 == 378758
				${If} $4 == 5
					${If} $5 <= 1
						Goto doneSucceed
					${EndIf}
				${EndIf}
				Goto doneFail
			${EndIf}
			#.NET Framework 4.5.2
			${If} $2 == 379893
				${If} $4 == 5
					${If} $5 <= 2
						Goto doneSucceed
					${EndIf}
				${EndIf}
				Goto doneFail
			${EndIf}
			#.NET Framework 4.6 RC
			${If} $2 == 393273
				${If} $4 <= 6
					Goto doneSucceed
				${EndIf}
				Goto doneFail
			${EndIf}
		#end loop
	#end fourFive

	doneFail:
		Push 0 #Result
		Goto done
	doneSucceed:
		Push 1 #Result
	done:
FunctionEnd