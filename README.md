---
Development Guide for Virtual RTU
---

Steps
=====

The following steps are required to get the Virtual RTU working in debug mode on
your local machine.

1.  Create Azure resources

2.  Deploy Piraeus in AKS

3.  Configure VRTU projects

4.  Deploy a new device using a PS script

5.  Auto-configure the IoT Edge device

6.  Run the solution end-2-end in debug mode

Step 1 – Getting Ready
----------------------

You will need to update your local machine with the latest tooling. You will
need to install these
[prereqs](https://github.com/skunklab/piraeus/blob/master/docs/prereqs.md) as
well as the following:

-   Visual Studio 2019 v16.3.10

-   [Visual Studio 2019
    Preview](https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=enterprise&ch=pre&rel=16)

    -   Note: We need to also install the preview version to leverage dotnet
        core v3.0.1 with Azure Functions and their dependencies.

Step 2- Deploy Piraeus in AKS
-----------------------------

1.  Clone the source <http://github.com/skunklab/piraeus> onto your local
    machine.

2.  Following the instructions in the ReadMe.md
    ([here](https://github.com/skunklab/piraeus)) to create the deployment and
    configuration file. The deployment should take between 15-20 minutes to
    complete.

3.  The resulting deployment will create a configuration file in the
    “kubernetes” folder in the form “*dd-mm-yyyyThh-mm-ss.json*”. We will use
    this file in the next step.

Step 3- Creating the VRTU configuration file
--------------------------------------------

1.  Clone the VRTU source <https://github.com/skunklab/virtualrtu>

2.  Open a new command prompt and navigate to the “Kubernetes” folder on the
    virtual rtu source.

3.  Type “*pwsh*” to start PowerShell

4.  Type “*. ./New-VrtuConfigFile.ps1*” to load PowerShell command to create a
    configuration file.

5.  Type New-VrtuConfigFile with the following parameters to create the
    “vrtu-config.json” file.

| \-Dns             | Dns name for the vrtu, e.g., “growlingdog7”                                                                |
|-------------------|------------------------------------------------------------------------------------------------------------|
| \-VirtualRtuId    | Lowercase name of the VRTU. Use “testvrtu” for the sample.                                                 |
| \-ClusterName     | The AKS cluster name for the vrtu. If omitted “vrtucluster” is used.                                       |
| \-StorageAcctName | Name of a new Azure Storage Account used by the VRTU.                                                      |
| \-InFile          | This is the path to the configuration file created in the Step 2.                                          |
| \-OutFile         | The name of the configuration file to be output in the current “Kubernetes” folder. Use “vrtu-config.json” |

6.  Type “*. ./New-LocalDebugSetup.ps1*” to load a PowerShell command to
    configuration the solution locally.

7.  Type “*New-LocalDebugSetup*” with the following parameters

| \-File            | This will be the configuration file created in Step 2.5, i.e., “vrtu-config.json” |
|-------------------|-----------------------------------------------------------------------------------|
| \-IoTHubName      | Name of an IoTHub to be created.                                                  |
| \-AppInsightsName | Name of Application Insights instance to be created.                              |

The command will configure the solution and produce and output file in the
“kubernetes” folder in the form “*mm-dd-yyyyThh-mm-ss.json*” that can be used to
deploy a device to the IoT Hub.

Step 4- Deploying a Device to the IoT Hub
-----------------------------------------

1.  Open the “Build.sln” in the “src” folder using Visual Studio Preview.

2.  Set the “AzureIoT.Deployment.Function” project as the start project.

3.  Run the project in Visual Studio in debug mode.

4.  Back to the command prompt…type “*. ./New-DeviceDeploy.ps1*” to load the
    PowerShell to deploy a new device.

    Type “*New-DeviceDeploy*”. The follow is a parameter list for the command,
    but you will only need the 1st parameter. Your command should look like
    *New-DeviceDeploy -File “dd-mm-yyyyThh-mm-ss.json”.*

| \-File   | This will be the configuration file in the Kubernetes folder created in Step 2.7 in the form “mm-dd-yyyyThh-mm-ss.json”                              |
|----------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| \-Url    | Url of the Azure deployment function. You can omit this parameter which uses the default value of "*http://localhost:7071/api/DeploymentFunction*"   |
| \-Key    | “code” parameter used to call an Azure Function with http trigger. You can omit because we are uses the local function.                              |
| \-Update | \$true or \$false (default) indicates whether the command is for a new device and an update to an existing device. You can omit and use the default. |

5.  After the PowerShell command executes it will return a device connection
    string. Stop the Visual Studio debugger. Open the Azure Portal.

    1.  Find the resource group you created and select the newly created IoT Hub

    2.  Navigate down the menu in the IoT Hub and select the “IoT Edge” menu
        item.

    3.  Select the device, “device1”, you just created.

    4.  Copy the “primary connection string” and paste it in Notepad. Add
        “ModuleId=fieldgateway;” to the connection string. It should now look
        like (below) copy it to the clipboard.

        HostName=randombirdhub.azure-devices.net;DeviceId=device1;ModuleId=fieldgateway;SharedAccessKey=WgeWdRz2glOsSqF9h5dY6oeiP8H2EreTtYn85kmlIuo=

    5.  In Visual Studio right-click the *VirtualRtu.Module* project and select
        “*properties*” from the menu. Navigate to the Environment Variable
        section and paste the connection string in the value for the variable
        MODULE_CONNECTIONSTRING key.

6.  In Visual Studio set the solution to “Multiple Startup Projects”, which is
    preconfigured and start the solution in debug mode. The module should
    configure itself and the solution is ready to use. Using the “Echo Client”
    console application you should be able to send and receive messages.

