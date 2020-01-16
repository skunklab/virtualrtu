start /wait cmd /c choco install kubernetes-cli --version=1.15.3 all -y
start /wait cmd /c choco install kubernetes-helm --version=2.16.0 all -y
start /wait cmd /c choco install -y docker-for-windows --no-progress --fail-on-standard-error all -y
start /wait cmd /c choco install visualstudio2019professional-preview --pre all -y
start /wait cmd /c dotnet tool install --global PowerShell