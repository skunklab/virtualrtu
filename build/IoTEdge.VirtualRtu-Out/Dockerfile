FROM microsoft/dotnet:2.2-sdk AS build-env
FROM microsoft/dotnet:2.2-runtime
WORKDIR /app


ENTRYPOINT ["dotnet", "IoTEdge.VirtualRtu.dll"]
COPY . /app