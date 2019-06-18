FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app
EXPOSE 8881

ENTRYPOINT ["dotnet", "IoTEdge.VirtualRtu.WebApp.dll"]
COPY . /app
