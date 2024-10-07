@echo off
taskkill /im powershell.exe /f >nul
start /B powershell -WindowStyle Hidden -Command "Add-Type -Path '../../ConvergenceER/Convergence/CMI.dll'; [CMI.CMI]::Main()"