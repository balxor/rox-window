@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
%CSC% /nologo /target:winexe /out:"ROX Window Tool.exe" ^
  /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  RoxCompact.cs BuildInfo.cs
if errorlevel 1 (echo BUILD FAILED & exit /b 1)
echo Built: ROX Window Tool.exe