msbuild ..\src\IoTEdge.VirtualRtu\IoTEdge.VirtualRtu.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\IoTEdge.VirtualRtu\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\IoTEdge.VirtualRtu.log_errors;errorsonly /flp2:logfile=.\BuildOutput\IoTEdge.VirtualRtu_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\IoTEdge.VirtualRtu.log

dotnet publish "..\src\IoTEdge.VirtualRtu\IoTEdge.VirtualRtu.csproj" -c Release -o "..\..\build\IoTEdge.VirtualRtu-Out"

docker build -t iotedge-vrtu ./IoTEdge.VirtualRtu-Out


docker tag iotedge-vrtu skunklab/iotedge-vrtu

docker push skunklab/iotedge-vrtu