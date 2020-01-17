start /wait cmd /c choco install kubernetes-cli --version=1.15.3 all -y
powershell.exe Invoke-WebRequest -Uri https://github.com/PowerShell/PowerShell/releases/download/v7.0.0-rc.2/PowerShell-7.0.0-rc.2-win-x64.msi -OutFile .\PowerShell-7.0.0-rc.2-win-x64.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I PowerShell-7.0.0-rc.2-win-x64.msi /quiet'
start /wait cmd /c choco install kubernetes-helm --version=2.16.0 all -y
start /wait cmd /c choco install -y docker-for-windows --no-progress --fail-on-standard-error all -y
start /wait cmd /c choco install visualstudio2019professional-preview --pre all -y
