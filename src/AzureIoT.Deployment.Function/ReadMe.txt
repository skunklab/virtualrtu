Configuration

The "secrets.json" must be configured to either test locally.  The following is a definition of the parameters

hostname - The Piraeus hostname used for communication, e.g., "flyingfish14.eastus.cloudapp.azure.com"
storageConnectionString - Azure storage connection string, i.e., the storage account where the deployment 
                          and configuration information will be stored.
tableName - The Azure storage table name where the configuration information will be stored in the storage account.
defaultIoTHubConnectionString - The IoT Hub connection used as the default for deployment.
serviceUrl - The URL of the Configuration Azure Function.  
defaultTemplate - This is the default deployment template, which is the JSON template of the deployment and Base64 encoded.

You can run a deployment using the Powershell script NewDeviceDeploy.ps1 in the kubernetes folder.
(1) Open a command prompt and navigate to the "kubernetes" folder in the source.
(2) Open PowerShell Core, type "pwsh"
(3) Load the deployment script by typing ". ./newdevicedeploy.ps1"
(4) Type the following 
    New-DeviceDeploy -File "file-name-of-the-deploy.json-configuration" -Url "url-of-the-deployment-azure-function"

This will deploy a new device in the default IoT Hub and return the IoT Edge device connection string to use 
to configure the new device.





