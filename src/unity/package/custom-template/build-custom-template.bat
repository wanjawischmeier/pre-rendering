@@echo off
setlocal

rem Parse JSON: https://stackoverflow.com/a/36375415/13215204
set string={ "other": 1234, "year": 2016, "value": "str", "time": "05:01" }

rem Remove quotes
set string=%string:"=%
rem Remove braces
set "string=%string:~2,-2%"
rem Change colon+space by "]equal-sign"
set "string=%string:: =]=%"
rem Separate parts at comma into individual array assignments
set "string[%string:, =" & set "string[%"

rem echo %string[year]%

set FILE=< test.txt
echo %FILE%

rem set SEVENZ="S:\Program Files\7-Zip\7z"
rem set TMPLT_FOLDER="S:\Program Files\Unity\2020.3.16f1\Editor\Data\Resources\PackageManager\ProjectTemplates"
rem set PCKG_FOLDER="S:\users\wanja\Dokumente\pre-rendering\master\src\unity-package\custom-template\com.unity.template.3d-5.0.4\package"
rem set PCKG_NAME="3dtest2"
rem set TGT_PATH=%TMPLT_FOLDER%\com.unity.template.%PCKG_NAME%.tgz
rem 
rem del TGT_PATH
rem %SEVENZ% a -ttar %TGT_PATH% %PCKG_FOLDER%