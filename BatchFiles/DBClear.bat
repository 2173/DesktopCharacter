echo off

SET DataBase="db.sqlite"

choice /c:yn /m "y=���s n=�I��"

if %errorlevel% == 1 goto y
if %errorlevel% == 2 goto n

:y
del %~dp0..\DesktopCharacter\bin\x64\Debug\%DataBase%
del %~dp0..\DesktopCharacter\bin\x64\Release\%DataBase% 
echo Database���폜���܂���
GOTO END

:n
echo �I�����܂�
GOTO END

:END
pause