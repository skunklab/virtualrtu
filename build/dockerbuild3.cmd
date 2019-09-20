msbuild ..\src\IoTEdge.VirtualRtu.FieldGateway\IoTEdge.VirtualRtu.FieldGateway.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\IoTEdge.VirtualRtu.FieldGateway\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\IoTEdge.VirtualRtu.FieldGateway.log_errors;errorsonly /flp2:logfile=.\BuildOutput\IoTEdge.VirtualRtu.FieldGateway_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\IoTEdge.VirtualRtu.FieldGateway.log

dotnet publish "..\src\IoTEdge.VirtualRtu.FieldGateway\IoTEdge.VirtualRtu.FieldGateway.csproj" -c Release -o "..\..\build\IoTEdge.VirtualRtu.FieldGateway-Out"

docker rmi iotedge-vrtu-fieldgateway

docker build -t iotedge-vrtu-fieldgateway ./IoTEdge.VirtualRtu.FieldGateway-Out

docker tag iotedge-vrtu-fieldgateway skunklab/iotedge-vrtu-fieldgateway

docker push skunklab/iotedge-vrtu-fieldgateway