@echo off
SET SolutionDir="./"
SET ProjectName="CMI"

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

REM Rebuild the project
%MSBuildPath%MSBuild.exe %SolutionDir%\%ProjectName%.sln /t:Rebuild /p:Configuration=Release
IF ERRORLEVEL 1 (
    echo Build failed. Check the output for details.
    pause
    exit /b 1
)

echo Build completed successfully.
pause