@echo off

set BASE_DIR=C:\Users\dimas\Code\Volgatech\Automates\AllLabs
set /p REGEX=Set Regex: 

"./RegexToNFA/bin/Debug/net8.0/RegexToNFA.exe" "%BASE_DIR%\outputRegexToNFA.csv" "%REGEX%"
IF %ERRORLEVEL% NEQ 0 (
    echo Ошибка при выполнении RegexToNFA.exe
    exit /b %ERRORLEVEL%
)

"./DetermineNFA/bin/Debug/net8.0/DetermineNFA.exe" "%BASE_DIR%\outputRegexToNFA.csv" "%BASE_DIR%\outputDetermineNFA.csv"
IF %ERRORLEVEL% NEQ 0 (
    echo Ошибка при выполнении DetermineNFA.exe
    exit /b %ERRORLEVEL%
)

"./Minimize/bin/Debug/net8.0/Minimize.exe" "moore" "%BASE_DIR%\outputDetermineNFA.csv" "%BASE_DIR%\outputMinimize.csv" 
IF %ERRORLEVEL% NEQ 0 (
    echo Ошибка при выполнении Minimize.exe
    exit /b %ERRORLEVEL%
)

python "./visualizer/visualizer.py" "%BASE_DIR%\outputRegexToNFA.csv" "%BASE_DIR%\outputDetermineNFA.csv" "%BASE_DIR%\outputMinimize.csv"
IF %ERRORLEVEL% NEQ 0 (
    echo Ошибка при выполнении visualizer.py
    exit /b %ERRORLEVEL%
)