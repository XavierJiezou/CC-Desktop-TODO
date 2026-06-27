@echo off
REM Build with the C# compiler bundled in Windows (.NET Framework) - no SDK / Visual Studio needed.
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

set OUTDIR=%~dp0..\build
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

"%CSC%" /nologo /target:winexe /optimize+ /out:"%OUTDIR%\DesktopTodo.exe" ^
  /reference:System.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  "%~dp0Program.cs"

if errorlevel 1 (
  echo.
  echo [FAILED] compile error
  exit /b 1
)

echo.
echo [OK] built: %OUTDIR%\DesktopTodo.exe
endlocal
