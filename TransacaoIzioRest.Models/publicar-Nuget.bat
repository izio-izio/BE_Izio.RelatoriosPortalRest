@echo off 
del /s /q /f *.nupkg
setlocal enableextensions 
REM No echo hugao experience, coloca o primeiro comando, o outpur dele vai ser gravado na variavel myvar
for /f "tokens=*" %%a in ( 
'nuget pack '
) do ( 
set myvar=%%a 
) 
REM No lugar do echo abaixo vc coloca o novo comando + o conteúdo da variável myvar 
FOR %%F  IN (%myvar:~30,-3%) do (set myvar=%%~nxF )
nuget.exe push  %myvar%  -ApiKey izio-nuget -source https://api.izio.com.br/NugetServer/nuget
pause
endlocal 