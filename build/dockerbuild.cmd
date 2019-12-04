
::msbuild ..\src\AzureIoT.Deployment.Function\AzureIoT.Deployment.Function.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\AzureIoT.Deployment.Function\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\AzureIoT.Deployment.Function_errors.log;errorsonly /flp2:logfile=.\BuildOutput\AzureIoT.Deployment.Function_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\AzureIoT.Deployment.Function.log

::msbuild ..\src\VirtualRtu.Configuration.Function\VirtualRtu.Configuration.Function.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\VirtualRtu.Configuration.Function\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\VirtualRtu.Configuration.Function_errors.log;errorsonly /flp2:logfile=.\BuildOutput\VirtualRtu.Configuration.Function_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\VirtualRtu.Configuration.Function.log

::msbuild ..\src\IoTEdge.VirtualRtu.WebMonitor\IoTEdge.VirtualRtu.WebMonitor.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\IoTEdge.VirtualRtu.WebMonitor\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\IoTEdge.VirtualRtu.WebMonitor_errors.log;errorsonly /flp2:logfile=.\BuildOutput\IoTEdge.VirtualRtu.WebMonitor_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\IoTEdge.VirtualRtu.WebMonitor.log

::msbuild ..\src\VirtualRtu.Gateway\VirtualRtu.Gateway.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\VirtualRtu.Gateway\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\VirtualRtu.Gateway_errors.log;errorsonly /flp2:logfile=.\BuildOutput\VirtualRtu.Gateway_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\VirtualRtu.Gateway.log

::msbuild ..\src\VirtualRtu.Module\VirtualRtu.Module.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\VirtualRtu.Module\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\VirtualRtu.Module_errors.log;errorsonly /flp2:logfile=.\BuildOutput\VirtualRtu.Module_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\VirtualRtu.Module.log


dotnet build "..\src\AzureIoT.Deployment.Function\AzureIoT.Deployment.Function.csproj" -c Release -f netcoreapp3.0 -v n -o "./BuildOutput/AzureIoT.Deployment.Function" --force

dotnet build "..\src\VirtualRtu.Configuration.Function\VirtualRtu.Configuration.Function.csproj" -c Release -f netcoreapp3.0 -v n -o "./BuildOutput/VirtualRtu.Configuration.Function" --force

dotnet build "..\src\IoTEdge.VirtualRtu.WebMonitor\IoTEdge.VirtualRtu.WebMonitor.csproj" -c Release -f netcoreapp3.0 -v n -o "./BuildOutput/IoTEdge.VirtualRtu.WebMonitor" --force

dotnet build "..\src\VirtualRtu.Gateway\VirtualRtu.Gateway.csproj" -c Release -f netcoreapp3.0 -v n -o "./BuildOutput/VirtualRtu.Gateway" --force

dotnet build "..\src\VirtualRtu.Module\VirtualRtu.Module.csproj" -c Release -f netcoreapp3.0 -v n -o "./BuildOutput/VirtualRtu.Module" --force

dotnet publish "..\src\AzureIoT.Deployment.Function\AzureIoT.Deployment.Function.csproj" -c Release -f netcoreapp3.0 -o "AzureIoT.Deployment.Function" 

dotnet publish "..\src\VirtualRtu.Configuration.Function\VirtualRtu.Configuration.Function.csproj" -c Release -f netcoreapp3.0 -o "VirtualRtu.Configuration.Function" 

dotnet publish "..\src\IoTEdge.VirtualRtu.WebMonitor\IoTEdge.VirtualRtu.WebMonitor.csproj" -c Release -f netcoreapp3.0 -o "IoTEdge.VirtualRtu.WebMonitor" 

dotnet publish "..\src\VirtualRtu.Gateway\VirtualRtu.Gateway.csproj" -c Release -f netcoreapp3.0 -o "VirtualRtu.Gateway" 

dotnet publish "..\src\VirtualRtu.Module\VirtualRtu.Module.csproj" -c Release -f netcoreapp3.0 -o "VirtualRtu.Module"

CALL :remove "virtualrtu-gateway:v3.0"
CALL :remove "virtualrtu-fieldgateway:v3.0"
CALL :remove "skunklab/virtualrtu-gateway:v3.0"
CALL :remove "skunklab/virtualrtu-fieldgateway:v3.0"

docker build -t virtualrtu-gateway:v3.0 ./VirtualRtu.Gateway
docker build -t virtualrtu-fieldgateway:v3.0 ./VirtualRtu.Module

docker tag virtualrtu-gateway:v3.0 skunklab/virtualrtu-gateway:v3.0
docker tag virtualrtu-fieldgateway:v3.0 skunklab/virtualrtu-fieldgateway:v3.0

docker push skunklab/virtualrtu-gateway:v3.0
docker push skunklab/virtualrtu-fieldgateway:v3.0

ECHO "done"
EXIT /B %ERRORLEVEL%

:remove
SET nameId=""
for /f %%i in ('docker images -q "%~1"') DO SET nameId=%%i

ECHO " %nameId% "

IF %nameId% == "" (
    ECHO not found
) ELSE (
    ECHO removing image "%~1"
    docker rmi %~1
    SET nameId=""
    SET image=""
)
EXIT /B 0


