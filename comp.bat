@echo off
setlocal
set "csc_path=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
echo [BUILD] Compiling AmazeLagUltra Full Coverage...
"%csc_path%" /target:winexe /win32manifest:app.manifest /out:ALPA.exe /reference:System.Windows.Forms.dll,System.dll,System.Drawing.dll,System.Management.dll,Microsoft.Diagnostics.Tracing.TraceEvent.dll,netstandard.dll *.cs
if %errorlevel% neq 0 (
    pause
) else (
    echo [SUCCESS] AmazeLagUltra.exe is ready!
    pause
)