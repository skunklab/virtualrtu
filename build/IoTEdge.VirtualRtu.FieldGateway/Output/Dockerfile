FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app
EXPOSE 8888

ENTRYPOINT ["dotnet", "IoTEdge.VirtualRtu.FieldGateway.dll"]
COPY . /app
