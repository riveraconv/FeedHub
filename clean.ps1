adb kill-server
taskkill /F /IM adb.exe
taskkill /F /IM MSBuild.exe  
taskkill /F /IM dotnet.exe
taskkill /F /IM java.exe
Remove-Item -Recurse -Force "F:\Proyectos de CSHARP\FeedHub_Solution\FeedHub_App\obj"
Remove-Item -Recurse -Force "F:\Proyectos de CSHARP\FeedHub_Solution\FeedHub_App\bin"
adb start-server
adb uninstall com.companyname.feedhub_app