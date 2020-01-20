Deploying Virtual RTU
===


The following tasks and steps will create the [infrastructure](./images/deployment.png) needed for VRTU.
Video available [here](./images/Task1.mp4) for environmental setup.

Task 1 – Create VM and get the source
---

This task will (i) create environment for the deployment and (ii) get the source
onto the virtual machine for the deployment.

1.  Create a Win 10 VM in Azure

2.  Logon the VM and get the source
    [https://github.com/skunklab/virtualrtu](https://nam06.safelinks.protection.outlook.com/?url=https%3A%2F%2Fgithub.com%2Fskunklab%2Fvirtualrtu&data=02%7C01%7CMatt.Long%40microsoft.com%7C31325619afcf4f0db01708d79b9dfe12%7C72f988bf86f141af91ab2d7cd011db47%7C1%7C0%7C637148975178588567&sdata=R0Bp6tM%2BkdTMz93swpvmLiUiuifjViz82E70%2Bo22TPk%3D&reserved=0)
    on the VM

Task 2 – Configure the VM Environment
---

This task will configure the virtual machine with the correct Kubernetes
tooling, Helm, PowerShell 7, Azure CLI, Azure SDK, Visual Studio 2019 Preview,
and Docker

1.  Using File Explorer on the VM navigate to the “kubernetes” in the source.

2.  Right-click the “setupenv.cmd” and select “run as administrator”.  This will
    take \~7 minutes to complete.

3.  Right-click the “setupenv2.cmd” and select “run as administrator”.  The will
    take \~15 minutes to complete.

>   The environment is now prepared

Task 3 – Get PowerShell Modules loaded
---

This task will load the PowerShell modules needed for deployment onto the
virtual machine.

1.  Open a command prompt and navigate to the "kubernetes" folder in the source.
2. Type **pwsh-preview**  This will open PowerShell 7 command prompt.

3.  Type **Install-Module SkunkLabVirtualRtu**  This will install the PowerShell
    modules needed for deployment.

>   You can type **Get-Module -ListAvailable** and see the new modules loaded,
>   i.e., SkunkLabVirtualRtu, SkunkLabVrtuFunctions, SkunkLabAks, SkunkLabAzure,
>   and SkunkLabPiraeus

Task 4 – Deploy the Full Environment
---

This task will deploy the environment into Azure, which includes Piraeus, a VRTU
in a VNET, Diagnostic Monitor Web Application, and 2 Azure Functions

1.  Open a new PowerShell command prompt as done in 3.1

2.  Navigate to the “kubernetes” folder in the source using the command prompt

3.  Type **az login** and log into the Azure Portal 

4.  Type **New-FullVrtuDeploy** with following parameters as describe as well as
    an example of the command

| **Parameter**            | **Description**                                                                                                                                                                                                                     |
|--------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| \-Path                   | Use “.” as you should be in the “kubernetes” folder, which is important.                                                                                                                                                            |
| \-File                   | File name with “.json” which will contain all your deployment results, e.g., “mydeployment.json”                                                                                                                                    |
| \-SubscriptionName       | Name of the Azure subscription you will use for deployment.                                                                                                                                                                         |
| \-ResourceGroupName      | Name of the Azure resource group you will use for the deployment.                                                                                                                                                                   |
| \-Location               | Location of the Azure data center used for the deployment, e.g., “eastus”                                                                                                                                                           |
| \-Email                  | Use email address, which is used by Let’s Encrypt when generating valid certificates during deployment.                                                                                                                             |
| \-PiraeusDns             | DNS name of the Piraeus cluster.  The FQDN will be \<PiraeusDns\>.\<Location\>.cloudapp.azure.com                                                                                                                                   |
| \-MonitorDns             | DNS name of the Diagnostics monitoring web application.  The FQDN will be \<MonitorDns\>.\<Location\>.cloudapp.azure.com                                                                                                            |
| \-VirtualRtuId           | Name of the Virtual RTU, e.g., “testvrtu”                                                                                                                                                                                           |
| \-PiraeusClusterName     | Name of the Piraeus AKS cluster, e.g., “realiftpiraeuscluster”                                                                                                                                                                      |
| \-VrtuClusterName        | Name of the VRTU AKS cluster, e.g., “testvrtucluster”                                                                                                                                                                               |
| \-MonitorClusterName     | Name of the Diagnostics Monitor Cluster, e.g., “realiftmonitorcluster”                                                                                                                                                              |
| \-OrleansStorageAcctName | Name of the Azure storage account to be created for the Orleans cluster                                                                                                                                                             |
| \-VrtuStorageAcctName    | Name of the VRTU storage account to be created                                                                                                                                                                                      |
| \-Domain                 | This is your corporate domain name associated with Azure Active Directory, e.g., “microsoft” if you are a Microsoft FTE; otherwise use your corporate domain name.                                                                  |
| \-Port                   | Use 8080 as this is the default for the monitoring web app (no quotes)                                                                                                                                                              |
| \-IoTHubName             | Name of the IoTHub to be created is it does not already exist.                                                                                                                                                                      |
| \-ConfigAppName          | Name of the configuration Azure Function, e.g., “myconfigfunc”                                                                                                                                                                      |
| \-DeployAppName          | Name of the device deployment Azure Function, e.g., “mydeployfunc”                                                                                                                                                                  |
| \-DeployTemplatePath     | You should have a deployment json file from IoT Hub that encodes all the deployment information for containers and create options.  Create this file and place it in the “kubernetes” folder.  Use the file name for the parameter. |
| \-AppID                  | (Optional) The appId of an existing Service Principal if one exists.  If the parameter is omitted a new Service Principal will be created.                                                                                          |
| \-Password               | (Optional) The password of an existing Service Principal if one exists.  If the parameter is omitted a new Service Principal will be created.                                                                                       |
| \-LogLevel               | The logging level of the one of “Error”, “Warning”, “Information”, or “Debug”                                                                                                                                                       |

**Example**

New-FullVrtuDeploy -Path "." -File "bitrt57.json" -SubscriptionName
"malong-mlprivate" -ResourceGroupName "testdeploy" -Location "eastus" -Email
"<myemail@gmail.com>" -PiraeusDns "bitrt57" -MonitorDns "bitrt57monitor"
-VirtualRtuId "testvrtu" -PiraeusClusterName "bitrt57cluster" -VrtuClusterName
"bitrt75vrtucluster" -MonitorClusterName "bitrt57monitorcluster"
-OrleansStorageAcctName "orleansgrains" -VrtuStorageAcctName "vrtustorage"
-Domain "microsoft" -Port 8080 -IoTHubName "vtruhub" -ConfigAppName
"vrtuconfigfunc" -DeployAppName "vrtudeployfunc" -DeployTemplatePath
"deploy-template.json" -LogLevel "Warning"

Note:

This full deployment should take between 1 hour and 1 hr and 20 minutes.

