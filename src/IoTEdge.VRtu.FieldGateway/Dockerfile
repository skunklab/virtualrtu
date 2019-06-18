FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app
EXPOSE 8877

ENTRYPOINT ["dotnet", "IoTEdge.Vrtu.FieldGateway.dll"]
COPY . /app