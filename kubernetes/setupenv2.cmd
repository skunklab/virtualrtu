start /wait cmd /c choco install -y docker-desktop --no-progress --fail-on-standard-error all -y
start /wait cmd /c choco install kubernetes-cli all -y
start /wait cmd /c choco install kubernetes-helm --version=3.1.2 all -y
start /wait cmd /c choco install visualstudio2019professional-preview --pre all -y