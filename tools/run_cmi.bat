@echo off 
start /B powershell -WindowStyle Hidden -Command "Add-Type -Path '../../ConvergenceER/mod/CMI.dll'; [CMI.CMI]::Main()" 
