$sources=@("https://api.nuget.org/v3/index.json","https://xpandnugetserver.azurewebsites.net/nuget","C:\Program Files\DevExpress 24.2\Components\System\Components\packages")   
& $PSScriptRoot\support\build\go.ps1 -taskList @("Release") -packageSources $sources -msbuildArgs @("/p:Configuration=Release","/WarnAsError","/v:m") -version "24.2.701.0" 
exit $LastExitCode


