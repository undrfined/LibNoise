@echo off
set nantfile=LibNoise.build
set nantexe=tools\nant\nant.exe
set NoPause=false

%nantexe% -nologo -buildfile:%nantfile% clean %1 %2 %3 %4 %5 %6 %7 %8
IF ERRORLEVEL 1 GOTO Failed

echo "CLEANED"
GOTO End

:Failed
echo "============================================================"
echo "CLEAN FAILED"
echo "============================================================"

IF NOT "%NoPause%"=="true" goto END 
exit /B 1

:End
if "%NoPause%"=="true" goto ENDBATCHFILE 
pause
:ENDBATCHFILE