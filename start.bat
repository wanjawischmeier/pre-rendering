start /d "C:\Program Files\Google\Chrome\Application" chrome.exe
start /d "S:\Program Files\Unity\2020.3.16f1\Editor" Unity.exe -projectPath %CD%\src\unity-concept
start /d "S:\Program Files\Microsoft Visual Studio\Common7\IDE" devenv.exe %CD%\src\unity-concept\unity-concept.sln
start /d "S:\Program Files\Microsoft VS Code" Code.exe %CD%
start /d "C:\Users\wanja\AppData\Local\GitHubDesktop" GitHubDesktop.exe
start /d "C:\Program Files\Blender Foundation\Blender 2.92" blender.exe