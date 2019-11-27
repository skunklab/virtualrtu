#cleanup script for source check in

    $path1 = "../src/IoTEdge.VirtualRtu.WebMonitor/secrets.json"
    $webAppConfig = Get-Content -Raw -Path $path1 | ConvertFrom-Json
    $webAppConfig.hostname = ""
    $webAppConfig.tableName = ""
    $webAppConfig.storageConnectionString = ""
    $webAppConfig.symmetricKey = ""
    $webAppConfig | ConvertTo-Json -depth 100 | Out-File $path1

    $path2 = "../src/AzureIoT.Deployment.Function/secrets.json"
    $deployFuncConfig = Get-Content -Raw -Path $path2 | ConvertFrom-Json
    $deployFuncConfig.hostname = ""
    $deployFuncConfig.storageConnectionString = ""
    $deployFuncConfig.tableName = ""
    $deployFuncConfig.serviceUrl = ""
    $deployFuncConfig.defaultTemplate = ""
    $deployFuncConfig.defaultIoTHubConnectionString = ""
    $deployFuncConfig | ConvertTo-Json -depth 100 | Out-File $path2    

    $path3 = "../src/VirtualRtu.Gateway/secrets.json"
    $gwConfig = Get-Content -Raw -Path $path3 | ConvertFrom-Json
    $gwConfig.hostname = ""
    $gwConfig.virtualRtuId = ""
    $gwConfig.instrumentationKey = ""
    $gwConfig.symmetricKey = ""
    $gwConfig.lifetimeMinutes = 0
    $gwConfig.storageConnectionString = ""
    $gwConfig.container = ""
    $gwConfig.filename = ""
    $gwConfig | ConvertTo-Json -depth 100 | Out-File $path3   

    $path4 = "../src/VirtualRtu.Configuration.Function/secrets.json"
    $funcConfig = Get-Content -Raw -Path $path4 | ConvertFrom-Json
    $funcConfig.symmetricKey = ""
    $funcConfig.apiToken = ""
    $funcConfig.lifetimeMinutes = 0
    $funcConfig.tableName = ""
    $funcConfig.storageConnectionString = ""
    $funcConfig.rtuMapContainer = ""
    $funcConfig.rtuMapFilename = ""
    $funcConfig | ConvertTo-Json -depth 100 | Out-File $path4
    
    $path5 = "./deploy.json"
    $deployConfig = Get-Content -Raw -Path $path5 | ConvertFrom-Json
    $deployConfig.virtualRtuId = ""
	$deployConfig.module.instrumentationKey = ""
    $deployConfig | ConvertTo-Json -depth 100 | Out-File $path5    

    $path6 = "../src/VirtualRtu.Module/Properties/launchSettings.json"
    $lsConfig = Get-Content -Raw -Path $path5 | ConvertFrom-Json
    $lsConfig.profiles.VirtualRtu.Module.environmentVariables.MODULE_CONNECTIONSTRING = ""
    $lsConfig | ConvertTo-Json -depth 100 | Out-File $path6
