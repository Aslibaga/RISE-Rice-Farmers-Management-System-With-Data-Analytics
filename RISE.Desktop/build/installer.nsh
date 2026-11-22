; Check and install SQL Server Express LocalDB
!macro customInstall
    ; Diagnostic: show message immediately to confirm macro runs
    MessageBox MB_OK "customInstall macro is running. INSTDIR=$INSTDIR"
    
    ; Try to open diagnostic log (non-fatal if it fails)
    StrCpy $9 "$TEMP\RISE_LocalDB_Diagnostic.log"
    FileOpen $2 $9 w
    
    ${If} $2 != ""
        FileWrite $2 "=== RISE LocalDB Diagnostic ===$\r$\n"
        FileWrite $2 "INSTDIR: $INSTDIR$\r$\n"
        FileWrite $2 "TEMP: $TEMP$\r$\n"
        FileWrite $2 "PROGRAMFILES: $PROGRAMFILES$\r$\n"
        FileWrite $2 "PROGRAMFILES64: $PROGRAMFILES64$\r$\n"
        FileWrite $2 "Checking for LocalDB...$\r$\n"
    ${EndIf}
    
    ; Check if LocalDB is already installed (64-bit paths first)
    IfFileExists "$PROGRAMFILES64\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe" LocalDBFound
    IfFileExists "$PROGRAMFILES64\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe" LocalDBFound
    IfFileExists "$PROGRAMFILES64\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe" LocalDBFound
    IfFileExists "$PROGRAMFILES\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe" LocalDBFound
    IfFileExists "$PROGRAMFILES\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe" LocalDBFound
    IfFileExists "$PROGRAMFILES\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe" LocalDBFound
    
    ${If} $2 != ""
        FileWrite $2 "LocalDB not found. Checking for bundled MSI...$\r$\n"
    ${EndIf}
    
    ; Verify MSI exists
    IfFileExists "$INSTDIR\resources\SqlLocalDB.msi" MSIExists
    ${If} $2 != ""
        FileWrite $2 "ERROR: MSI not found at $INSTDIR\resources\SqlLocalDB.msi$\r$\n"
        FileClose $2
    ${EndIf}
    MessageBox MB_OK "MSI not found at $INSTDIR\resources\SqlLocalDB.msi"
    Abort
    
    MSIExists:
    ${If} $2 != ""
        FileWrite $2 "MSI found. Starting installation via msiexec...$\r$\n"
    ${EndIf}
    
    ; Install LocalDB using msiexec
    ExecWait '"$SYSDIR\msiexec.exe" /i "$INSTDIR\resources\SqlLocalDB.msi" /qn /norestart /L*v "$TEMP\RISE_LocalDB_MSI.log"' $1
    
    ${If} $2 != ""
        FileWrite $2 "MSI exit code: $1$\r$\n"
        FileClose $2
    ${EndIf}
    
    ; Handle exit codes
    ${If} $1 == 0
        DetailPrint "LocalDB installed successfully."
    ${ElseIf} $1 == 3010
        DetailPrint "LocalDB installed successfully (reboot required)."
        MessageBox MB_OK "SQL Server Express LocalDB was installed successfully. A system restart may be required before using RISE."
    ${ElseIf} $1 == 1641
        DetailPrint "LocalDB installed successfully (restart initiated)."
        MessageBox MB_OK "SQL Server Express LocalDB was installed successfully. The system will restart."
    ${Else}
        MessageBox MB_OK "LocalDB installation failed (exit code $1).$\r$\n$\r$\nCheck logs at:$\r$\n$9$\r$\n$TEMP\RISE_LocalDB_MSI.log"
        Abort
    ${EndIf}
    
    Goto LocalDBDone
    
    LocalDBFound:
    ${If} $2 != ""
        FileWrite $2 "LocalDB already installed.$\r$\n"
        FileClose $2
    ${EndIf}
    DetailPrint "SQL Server Express LocalDB is already installed."
    
    LocalDBDone:
!macroend