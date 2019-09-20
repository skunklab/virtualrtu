
msbuild ..\src\IoTEdge.FieldGateway.Function\IoTEdge.FieldGateway.Function.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\IoTEdge.FieldGateway.Function\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\IoTEdge.FieldGateway.Function.log_errors;errorsonly /flp2:logfile=.\BuildOutput\IoTEdge.FieldGateway.Function_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\IoTEdge.FieldGateway.Function.log

msbuild ..\src\IoTEdge.VirtualRtu\IoTEdge.VirtualRtu.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\IoTEdge.VirtualRtu\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\IoTEdge.VirtualRtu.log_errors;errorsonly /flp2:logfile=.\BuildOutput\IoTEdge.VirtualRtu_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\IoTEdge.VirtualRtu.log

msbuild ..\src\IoTEdge.VirtualRtu.FieldGateway\IoTEdge.VirtualRtu.FieldGateway.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\IoTEdge.VirtualRtu.FieldGateway\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\IoTEdge.VirtualRtu.FieldGateway.log_errors;errorsonly /flp2:logfile=.\BuildOutput\IoTEdge.VirtualRtu.FieldGateway_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\IoTEdge.VirtualRtu.FieldGateway.log

msbuild ..\src\IoTEdge.VirtualRtu.WebApp\IoTEdge.VirtualRtu.WebApp.csproj /t:Clean,Rebuild,restore /p:OutputPath=..\..\build\IoTEdge.VirtualRtu.WebApp\Output /p:Configuration=Release /fl1 /fl2 /fl3 /flp1:logfile=.\BuildOutput\IoTEdge.VirtualRtu.WebApp.log_errors;errorsonly /flp2:logfile=.\BuildOutput\IoTEdge.VirtualRtu.WebApp_warnings.log;warningsonly /flp3:logfile=.\BuildOutput\IoTEdge.VirtualRtu.WebApp.log


dotnet publish "..\src\IoTEdge.FieldGateway.Function\IoTEdge.FieldGateway.Function.csproj" -c Release -o "..\..\build\IoTEdge.FieldGateway.Function-Out"

dotnet publish "..\src\IoTEdge.VirtualRtu\IoTEdge.VirtualRtu.csproj" -c Release -o "..\..\build\IoTEdge.VirtualRtu-Out"

dotnet publish "..\src\IoTEdge.VirtualRtu.FieldGateway\IoTEdge.VirtualRtu.FieldGateway.csproj" -c Release -o "..\..\build\IoTEdge.VirtualRtu.FieldGateway-Out"

dotnet publish "..\src\IoTEdge.VirtualRtu.WebApp\IoTEdge.VirtualRtu.WebApp.csproj" -c Release -o "..\..\build\IoTEdge.VirtualRtu.WebApp-Out"

#docker rmi iotedge-vrtu
#docker rmi iotedge-vrtu-fieldgateway
#docker rmi iotedge-vrtu-webapp

docker build -t iotedge-vrtu ./IoTEdge.VirtualRtu-Out
docker build -t iotedge-vrtu-fieldgateway ./IoTEdge.VirtualRtu.FieldGateway-Out
docker build -t iotedge-vrtu-webapp ./IoTEdge.VirtualRtu.WebApp-Out

docker tag iotedge-vrtu-fieldgateway skunklab/iotedge-vrtu-fieldgateway

docker push skunklab/iotedge-vrtu-fieldgateway


