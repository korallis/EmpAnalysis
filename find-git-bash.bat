@echo off
echo Looking for Git Bash installation...
echo.

if exist "C:\Program Files\Git\bin\bash.exe" (
    echo Found at: C:\Program Files\Git\bin\bash.exe
) else (
    echo Not found at: C:\Program Files\Git\bin\bash.exe
)

if exist "C:\Program Files (x86)\Git\bin\bash.exe" (
    echo Found at: C:\Program Files (x86)\Git\bin\bash.exe
) else (
    echo Not found at: C:\Program Files (x86)\Git\bin\bash.exe
)

if exist "%USERPROFILE%\AppData\Local\Programs\Git\bin\bash.exe" (
    echo Found at: %USERPROFILE%\AppData\Local\Programs\Git\bin\bash.exe
) else (
    echo Not found at: %USERPROFILE%\AppData\Local\Programs\Git\bin\bash.exe
)

echo.
echo Checking where command...
where bash 2>nul
where git 2>nul

pause 