@echo off
SET SolutionDir="../"
SET ProjectName="CMI"
SET HideWindow="true"

REM Attempt to find MSBuild for Visual Studio 2022 Community edition
SET MSBuildPath="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\"

REM Check if MSBuild exists
IF NOT EXIST %MSBuildPath%MSBuild.exe (
    echo MSBuild not found at %MSBuildPath%. Please install Visual Studio 2022 Community edition or check the path.
    pause
    exit /b 1
)

REM Change to the solution directory
cd /d %SolutionDir% || (
    echo Failed to change directory. Check the path.
    pause
    exit /b 1
)

REM Set the DefineConstants based on the HideWindow value
IF %HideWindow%=="true" (
    SET DefineConstants="HIDE_WINDOW"
) ELSE (
    SET DefineConstants=""
)

REM Rebuild the project
%MSBuildPath%MSBuild.exe %SolutionDir%\%ProjectName%.sln /t:Rebuild /p:Configuration=Release /p:DefineConstants=%DefineConstants%
IF ERRORLEVEL 1 (
    echo Build failed. Check the output for details.
    pause
    exit /b 1
)

echo Build completed successfully.
pause