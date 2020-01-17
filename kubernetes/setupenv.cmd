powershell.exe Set-ExecutionPolicy  -ExecutionPolicy Unrestricted -Scope LocalMachine
powershell.exe Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
powershell.exe Invoke-WebRequest -Uri https://download.visualstudio.microsoft.com/download/pr/854ca330-4414-4141-9be8-5da3c4be8d04/3792eafd60099b3050313f2edfd31805/dotnet-sdk-3.1.101-win-x64.exe -OutFile .\dotnet-sdk-3.1.101-win-x64.exe
start /wait dotnet-sdk-3.1.101-win-x64.exe /install /norestart /quiet
powershell.exe Install-PackageProvider -Name NuGet -Force
powershell.exe Install-Module -Name PowerShellGet -Force
powershell.exe Update-Module -Name PowerShellGet
start /wait cmd /c powershell.exe iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))