echo off

SET DataBase="db.sqlite"

choice /c:yn /m "y=実行 n=終了"

if %errorlevel% == 1 goto y
if %errorlevel% == 2 goto n

:y
del %~dp0..\DesktopCharacter\bin\x64\Debug\%DataBase%
del %~dp0..\DesktopCharacter\bin\x64\Release\%DataBase% 
echo Databaseを削除しました
GOTO END

:n
echo 終了します
GOTO END

:END
pause