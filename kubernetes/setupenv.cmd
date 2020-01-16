powershell.exe Set-ExecutionPolicy  -ExecutionPolicy Unrestricted -Scope LocalMachine
powershell.exe Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
powershell.exe Invoke-WebRequest -Uri https://download.visualstudio.microsoft.com/download/pr/66adfd75-9c1d-4e44-8d9c-cdc0cbc41104/5288b628601e30b0fa10d64fdaf64287/dotnet-sdk-3.0.101-win-x64.exe -OutFile .\dotnet-sdk-3.0.101-win-x64.exe
start /wait dotnet-sdk-3.0.101-win-x64.exe /install /norestart /quiet
powershell.exe Install-PackageProvider -Name NuGet -Force
powershell.exe Install-Module -Name PowerShellGet -Force
powershell.exe Update-Module -Name PowerShellGet
start /wait cmd /c powershell.exe iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))