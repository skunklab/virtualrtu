﻿function New-LocalDebugSetup()  
{	
    param ([string]$File)
    
    $config = Get-Content -Raw -Path $File | ConvertFrom-Json

    $subscriptionNameOrId = $config.subscriptionNameOrId
    $resourceGroupName = $config.resourceGroupName
    $location = $config.location
    $iotHubName = $IoTHubName
    $appInsightsName = $AppInsightsName
    $storageAcctName = $config.storageAcctName
    $virtualRtuId = $config.virtualRtuId    

    $kind = "other"
    $rtuMapcontainer = $config.containerName
    $rtuMapFilename = $config.filename
    $tableName = "gateway"
    $lifetimeMinutes = $config.lifetimeMinutes
    $serviceUrl = "http://localhost:7071/api/ConfigurationFunction"

    Write-Host "Defaults configured"

    az account set --subscription "$subscriptionNameOrId"
	if($LASTEXITCODE -ne 0)
	{
		Write-Host "Subscription - $subscriptionNameOrId could not be set" -ForegroundColor Yellow
		Write-Host "Exiting script" -ForegroundColor Yellow
		return;
	}

	Write-Host "" -ForegroundColor White
  
    $apiToken = $config.apiToken
    $hostname = $config.hostname
    $symmetricKey = $config.symmetricKey

    Write-Host "Parameters read from piraeus file"

    
    $instrumentationKey = $config.instrumentationKey
    $iotHubConnectionString = $config.iotHubConnectionString
    
    Write-Host "" -ForegroundColor White
    Write-Host "IoT Hub deployed"


    #get the debug-template and base64 encoded it
    $debugTemplateJson = Get-Content -Raw -Path "./debug-template.json"
    $base64Template=[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($debugTemplateJson))

    Write-Host "Default deployment template encoded"

    $saLine= az storage account check-name --name $storageAcctName
    if($saLine[2].Contains("true"))
    {
	    Write-Host "-- Step $step - Creating VRTU storage account" -ForegroundColor Green
	    az storage account create --location $location --name $storageAcctName --resource-group $resourceGroupName --sku "Standard_LRS"
    }
    else
    {
	    Write-Host "Storage Account Exists"
    } 


    $dcs = az storage account show-connection-string --name $storageAcctName --resource-group $resourceGroupName
    $vs1 = $dcs.Replace(",","").Replace(":","=").Replace(" ","").Replace('"',"").Replace("{","").Replace("}","").Trim()
    $ts1 = $vs1 -split "connectionString="
    $dataConnectionString = $ts1[2]  

    Write-Host "Storage account created"

    #update the Web App secrets.json
    $path1 = "../src/IoTEdge.VirtualRtu.WebMonitor/secrets.json"
    $webAppConfig = Get-Content -Raw -Path $path1 | ConvertFrom-Json
    $webAppConfig.hostname = $hostname
    $webAppConfig.tableName = $tableName
    $webAppConfig.storageConnectionString = $dataConnectionString
    $webAppConfig.symmetricKey = $symmetricKey
    $webAppConfig | ConvertTo-Json -depth 100 | Out-File $path1

    Write-Host "Updated secrets file in IoTEdge.VirtualRtu.WebMonitor"


    $path2 = "../src/AzureIoT.Deployment.Function/secrets.json"
    $deployFuncConfig = Get-Content -Raw -Path $path2 | ConvertFrom-Json
    $deployFuncConfig.hostname = $hostname
    $deployFuncConfig.storageConnectionString = $dataConnectionString
    $deployFuncConfig.tableName = $tableName
    $deployFuncConfig.serviceUrl = $serviceUrl
    $deployFuncConfig.defaultTemplate = $base64Template
    $deployFuncConfig.defaultIoTHubConnectionString = $iotHubConnectionString
    $deployFuncConfig | ConvertTo-Json -depth 100 | Out-File $path2

    Write-Host "Updated secrets file in AzureIoT.Deployment.Function"

    $path3 = "../src/VirtualRtu.Gateway/secrets.json"
    $gwConfig = Get-Content -Raw -Path $path3 | ConvertFrom-Json
    $gwConfig.hostname = $hostname
    $gwConfig.virtualRtuId = $virtualRtuId
    $gwConfig.instrumentationKey = $instrumentationKey
    $gwConfig.symmetricKey = $symmetricKey
    $gwConfig.lifetimeMinutes = $lifetimeMinutes
    $gwConfig.storageConnectionString = $dataConnectionString
    $gwConfig.container = $rtuMapcontainer
    $gwConfig.filename = $rtuMapFilename
    $gwConfig | ConvertTo-Json -depth 100 | Out-File $path3

    Write-Host "Updated secrets file in VirtualRtu.Gateway"

    $path4 = "../src/VirtualRtu.Configuration.Function/secrets.json"
    $funcConfig = Get-Content -Raw -Path $path4 | ConvertFrom-Json
    $funcConfig.symmetricKey = $symmetricKey
    $funcConfig.apiToken = $apiToken
    $funcConfig.lifetimeMinutes = $lifetimeMinutes
    $funcConfig.tableName = $tableName
    $funcConfig.storageConnectionString = $dataConnectionString
    $funcConfig.rtuMapContainer = $rtuMapcontainer
    $funcConfig.rtuMapFilename = $rtuMapFilename
    $funcConfig | ConvertTo-Json -depth 100 | Out-File $path4

    Write-Host "Updated secrets file in VirtualRtu.Configuration.Function"
    
    $dateTimeString = Get-Date -Format "MM-dd-yyyyTHH-mm-ss"
	$filename = "./deploy-" + $dateTimeString + ".json"

    $path5 = "./deploy.json"
    $deployConfig = Get-Content -Raw -Path $path5 | ConvertFrom-Json
    $deployConfig.virtualRtuId = $virtualRtuId
    $deployConfig.expiryMinutes = $lifetimeMinutes
	$deployConfig.module.instrumentationKey = $instrumentationKey
    $deployConfig | ConvertTo-Json -depth 100 | Out-File $filename
    
    Write-Host "Updated $filename file with virtualRtuId and App Insights instrumentationKey."
  
    Write-Host "Done Dare Mighty Things :-)" -ForegroundColor Green

}

