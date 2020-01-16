function New-FullVrtuDeploy
{
    param([string]$Path, [string]$File, [string]$SubscriptionName , [string]$ResourceGroupName , 
    [string]$Location, [string]$Email, [string]$PiraeusDns, [string]$MonitorDns, [string]$VirtualRtuId,
    [string]$PiraeusClusterName, [string]$VrtuClusterName, [string]$MonitorClusterName,
    [string]$OrleansStorageAcctName, [string]$VrtuStorageAcctName, [string]$Domain,
    [int]$Port, [string]$IoTHubName, [string]$ConfigAppName, [string]$DeployAppName, [string]$DeployTemplatePath, [string]$AppID, [string]$Password, [string]$LogLevel)
	
    $start = Get-Date
    New-PiraeusDeploy -Path $Path -File $File -SubscriptionName $SubscriptionName -ResourceGroupName $ResourceGroupName -Location $Location -Email $Email -Dns $PiraeusDns -ClusterName $PiraeusClusterName -AppID $AppID -Password $Password -OrleansStorageAcctName $OrleansStorageAcctName -LogLevel $LogLevel

    #read the config file
    $config = Get-Content -Raw -Path $File | ConvertFrom-Json
    $symmetricKey = $config.symmetricKey
    $hostname = $config.piraeusHostname
    $appId = $config.appId
    $pwd = $config.pwd

    New-VrtuVnetDeploy -Path $Path -File $File -SubscriptionName $SubscriptionName -ResourceGroupName $ResourceGroupName -Location $Location -VirtualRtuId $VirtualRtuId -Hostname $hostname -SymmetricKey $symmetricKey -IoTHubName $IoTHubName -StorageAcctName $VrtuStorageAcctName -ClusterName $VrtuClusterName -AppID $appId -Password $pwd -LogLevel $LogLevel  -StartTime $start
    New-WebMonitorDeploy -Path $Path  -File $File -SubscriptionName $SubscriptionName  -ResourceGroupName $ResourceGroupName  -Location $Location -PiraeusHostname $hostname -VirtualRtuId $VirtualRtuId  -StorageAcctName $VrtuStorageAcctName  -SymmetricKey $symmetricKey -Dns $MonitorDns -Email $Email -ClusterName $MonitorClusterName  -Domain $Domain -Port $Port  -AppID $appId -Password $pwd -LogLevel $LogLevel -StartTime $start   
    
    $funcUrl = New-DeployFunctions -Path $Path -File $File -ConfigAppName $ConfigAppName -DeployAppName $DeployAppName -DeployTemplatePath $DeployTemplatePath -Location $Location -ResourceGroupName $ResourceGroupName -SubscriptionName $SubscriptionName

	return $funcUrl
    
    
}

function New-DeviceDeploy
{
  param ([string]$File, [string]$Url = "http://localhost:7071/api/DeploymentFunction", [string]$Key = $null, [bool]$Update = $false)

    $config = [System.IO.File]::ReadAllBytes($File)
    
    $requestUrl = $null
    
    if($Key -eq $null)
    {
		if($Update -eq $false)
		{
			$requestUrl = $Url
		}
		else
		{
			$requestUrl = $Url + "?type=update" 
		}
    }
    else
    {
		$requestUrl = $Url + "?code=" + $Key
		if($Update -ne $false)
		{
			$requestUrl = $requestUrl + "&type=update"
		}
    }
    
    if($Module -eq $null)
    {
		$connectionString = Invoke-WebRequest -Uri $requestUrl -Method Post -ContentType "application/json" -Body $config
		Write-Host $connectionString
    }
    else
    {
		Invoke-WebRequest -Uri $requestUrl -Method Post -ContentType "application/json" -Body $config
		Write-Host "Update complete"				
    }
}

function New-DeployFunctions 
{
	param([string]$Path, [string]$File, [string]$ConfigAppName, [string]$DeployAppName, [string]$DeployTemplatePath, [string]$Location, [string]$ResourceGroupName, [string]$SubscriptionName)

	$base64Template = New-FileToBase64 -Path $Path -Filename $DeployTemplatePath
        $config = Get-Content -Raw -Path $File | ConvertFrom-Json
	$acctName = Get-AccountName -StorageConnectionString $config.vrtuConnectionString

        Update-ConfigSecrets -Path $Path -Folder "..\src\VirtualRtu.Configuration.Function" -SymmetricKey $config.symmetricKey -ApiToken $config.apiCode -LifetimeMinutes $config.lifetimeMinutes -TableName $config.tableName -StorageConnectionString $config.vrtuConnectionString -ContainerName  $config.containerName -Filename $config.filename
	New-FunctionApp -AppName $ConfigAppName -StorageAcctName $acctName -Location $Location -AppInsightsName "configfunc" -ResourceGroupName $ResourceGroupName
        New-ConfigurationFunctionApp -Path $Path -PublishFolder "../build/VirtualRtu.Configuration.Function/publish" -ZipFilename "configfunc.zip" -AppName $ConfigAppName -ResourceGroupName $ResourceGroupName
        $creds = Get-FunctionCreds -AppName $ConfigAppName -ResourceGroupName $ResourceGroupName -SubscriptionName $SubscriptionName
	$code = Get-FunctionCode -AppName $ConfigAppName -FunctionName "ConfigurationFunction" -EncodedCreds $creds
	$serviceUrl = "https://$ConfigAppName.azurewebsites.net/api/ConfigurationFunction?code=$code
	
	Update-DeploySecrets -Path $Path -Folder "..\src\AzureIoT.Deployment.Function" -Hostname $config.piraeusHostname -ServiceUrl $serviceUrl -TableName $config.tableName -StorageConnectionString $config.vrtuConnectionString -IoTHubConnectionString $config.iotHubConnectionString -Template $base64Template
	New-FunctionApp -AppName $DeployAppName -StorageAcctName $acctName -Location $Location -AppInsightsName "deployfunc" -ResourceGroupName $ResourceGroupName
	New-DeploymentFunctionApp -Path $Path -PublishFolder "../build/AzureIoT.Deployment.Function/publish" -ZipFilename "deployfunc.zip" -AppName $DeployAppName -ResourceGroupName $ResourceGroupName
	$creds = Get-FunctionCreds -AppName $DeployAppName -ResourceGroupName $ResourceGroupName -SubscriptionName $SubscriptionName
	$code2 = Get-FunctionCode -AppName $DeployAppName -FunctionName "DeploymentFunction" -EncodedCreds $creds
	return "https://$DeployAppName.azurewebsites.net/api/DeploymentFunction?code=$code2	
}

function Get-AccountName 
{
    param([string]$StorageConnectionString)

    $vals = $acct.Split(";")
    foreach($item in $vals)
    {
        if($item.Contains("AccountName="))
        {
            $acctNameParts = $item.Split("=")
            return $acctNameParts[1]
        }        
    }

    return $null
}

function Add-VrtuVnetDeploy
{
	param([string]$Path, [string]$File, [string]$SubscriptionName, [string]$ResourceGroupName, [string]$Location,
    [string]$VirtualRtuId, [string]$Hostname, [string]$SymmetricKey, [string]$BlobContainerName,
    [string]$TableName, [string]$IoTHubName,
    [string]$LogLevel, [string]$StorageAcctName, [int]$LifetimeMinutes = 0,
    [string]$ClusterName, [int]$NodeCount, [string]$VmSize,
    [string]$VnetName, [string]$SubnetName,
    [string]$VnetPrefix, [string]$SubnetPrefix,
    [string]$ServiceCidr, [string]$DnsServiceIP, [string]$PodCidr,
    [string]$DockerBridgeIP,     
    [string]$AppID, [string]$Password)

    $env:AZURE_HTTP_USER_AGENT='pid-332e88b9-31d3-5070-af65-de3780ad5c8b'

    $step = 0
    $start = Get-Date
    if($BlobContainerName.Length -eq 0)
    {
		$BlobContainerName = "maps"
    }
    
    if($TableName.Length -eq 0)
    {
		$TableName = "gateway"
    }
    
    if($LogLevel.Length -eq 0)
    {
		$LogLevel = "Information"
    }
    
    if($LifetimeMinutes -eq 0)
    {
		$LifetimeMinutes = 525600
    }
    
    if($NodeCount -eq 0)
    {
		$NodeCount = 1
    }
    
    if($VmSize.Length -eq 0)
    {
		$VmSize = "Standard_D2s_v3"
    }
    
    if($VnetName.Length -eq 0)
    {
		$VnetName = "vrtu-vnet"
    }
    
    if($SubnetName.Length -eq 0)
    {
		$SubnetName = "vrtu-subnet"
    }
    
    if($VnetPrefix.Length -eq 0)
    {
		$VnetPrefix = "192.168.0.0/16"
    }
    
    if($SubnetPrefix.Length -eq 0)
    {
		$SubnetPrefix = "192.168.1.0/24"
    }
    
    if($ServiceCidr.Length -eq 0)
    {
		$ServiceCidr = "10.0.0.0/16"
    }
    
    if($DnsServiceIP.Length -eq 0)
    {
		$DnsServiceIP = "10.0.0.10"
    }
    
    if($PodCidr.Length -eq 0)
    {
		$PodCidr = "10.244.0.0/16"
    }
    
    if($DockerBridgeIP.Length -eq 0)
    {
		$DockerBridgeIP = "172.17.0.1/16"
    }
    
    #default values
    $claimTypes = "http://$Hostname/name"
    $claimValues = $VirtualRtuId
    $issuer = "http://$Hostname/"
    $audience = $issuer
    $filename = "$VirtualRtuId.json"
    
    Write-Host "LTM = $LifetimeMinutes" -ForegroundColor Cyan
    $ltm = '\"'+ $LifetimeMinutes + '\"'
    
    #get the service principal
    Update-Step -Step $step -Message "Set Service Principal" -Start $start
    $step++
    $spn = Get-ServicePrincipal -AppID $AppID -Password $Password   
    $spnAppId = $spn."appId"
    $spnPwd = $spn."pwd"
    
    $iotHubConnection = Get-IoTHubConnectionString -HubName "$IoTHubName" -ResourceGroupName "$ResourceGroupName"

	if($iotHubConnection.Length -eq 0)
	{
		Write-Host "No IoT Hub connection string...stopping script" -ForegroundColor DarkCyan
		return
	}
    
    #create the storage account
    $storageAvailable = Get-StorageAccountNameAvailable -SubscriptionName "$SubscriptionName" -StorageAcctName "$StorageAcctName"   
    if($storageAvailable)
    {
        New-StorageAccount -StorageAcctName $StorageAcctName -Location $Location -ResourceGroupName $ResourceGroupName       
    }

    $storageConnectionString = Get-StorageAccountConnectionString -StorageAcctName $StorageAcctName -ResourceGroupName $ResourceGroupName
    if($LASTEXITCODE -ne 0)
    {
        Write-Host "Failed to get VRTU storage account connection string...terminating script." -ForegroundColor Yellow
        return
    }


    Update-Step -Step $step -Message "VRTU storage account connection string obtained" -Start $start
    $step++ 

    #create app insights and get instrumentation key
    Update-Step -Step $step -Message "Creating App Insights for VRTU cluster and getting instrumentation key" -Start $start
    $step++
    $instrumentationKey = Get-InstrumentationKey "$VirtualRtuId" -ResourceGroupName $ResourceGroupName -Location $Location
    
    Update-Step -Step $step -Message "Creating new VRTU VNET" -Start $start
    $step++

    az network vnet create --resource-group $ResourceGroupName --name $VnetName --address-prefixes 192.168.0.0/16 --subnet-name $SubnetName --subnet-prefix 192.168.1.0/24

    $VnetName_ID=$(az network vnet show --resource-group $ResourceGroupName --name $VnetName --query id -o tsv)
    $SubnetName_ID=$(az network vnet subnet show --resource-group $ResourceGroupName --vnet-name $VnetName --name $SubnetName --query id -o tsv)

    Update-Step -Step $step -Message "Assigning role to service principal" -Start $start
    $step++
    az role assignment create --assignee $spnAppId --scope $VnetName_ID --role Contributor

    Update-Step -Step $step -Message "Creating new VRTU AKS cluster" -Start $start
    $step++
    az aks create --resource-group $ResourceGroupName --name $ClusterName --node-count $NodeCount --network-plugin kubenet --service-cidr $ServiceCidr --dns-service-ip $DnsServiceIP --pod-cidr $PodCidr --docker-bridge-address $DockerBridgeIP --vnet-subnet-id $SubnetName_ID --service-principal "$spnAppId" --client-secret "$spnPwd"    
	kubectl config use-context $ClusterName
	
    Update-Step -Step $step -Message "Getting AKS credentials" -Start $start
    $step++
    az aks get-credentials --resource-group $ResourceGroupName --name $ClusterName

    Update-Step -Step $step -Message "Applying Helm RBAC" -Start $start
    $step++
    kubectl apply -f "./helm-rbac.yaml"

    Update-Step -Step $step -Message "Starting Tiller" -Start $start
    $step++
    helm init --service-account tiller
    Set-Timer -Message "...waiting 45 seconds for Tiller to start" -Seconds 45

    Update-Step -Step $step -Message "Deploying helm chart for VRTU VNET" -Start $start
    $step++
    helm install "$Path/virtualrtu-vnet" --name virtualrtu --namespace kube-system --set claimTypes=$claimTypes --set claimValues=$claimValues --set issuer=$issuer --set audience=$audience --set lifetimeMinutes=$ltm --set symmetricKey=$SymmetricKey --set hostname=$Hostname --set storageConnectionString="$storageConnectionString" --set container=$BlobContainerName --set filename=$filename --set virtualRtuId=$VirtualRtuId --set instrumentationKey=$instrumentationKey --set logLevel=$LogLevel

    Write-Host "-- Step $step - Geting IP for Subnet communications" -ForegroundColor Green
    $ip = Get-ExternalIPForService -AppName "vrtu"
    $step++

    Write-Host "-- IP = $ip " -ForegroundColor Magenta
    Write-Host "VRTU-VNET deployed!" -ForegroundColor Cyan

}


function New-VrtuVnetDeploy
{
	param([string]$Path, [string]$File, [string]$SubscriptionName, [string]$ResourceGroupName, [string]$Location,
    [string]$VirtualRtuId, [string]$Hostname, [string]$SymmetricKey, [string]$BlobContainerName,
    [string]$TableName, [string]$IoTHubName,
    [string]$LogLevel, [string]$StorageAcctName, [int]$LifetimeMinutes = 0,
    [string]$ClusterName, [int]$NodeCount, [string]$VmSize,
    [string]$VnetName, [string]$SubnetName,
    [string]$VnetPrefix, [string]$SubnetPrefix,
    [string]$ServiceCidr, [string]$DnsServiceIP, [string]$PodCidr,
    [string]$DockerBridgeIP,     
    [string]$AppID, [string]$Password, [int]$Step, [DateTime]$StartTime)
    
    $env:AZURE_HTTP_USER_AGENT='pid-332e88b9-31d3-5070-af65-de3780ad5c8b'

    if($Step -eq 0)
    {
		$step = 0
    }
    else
    {
		$step = $Step 
    }
    
    $step++
    
    $endTime = Get-Date
	$timeSpan = New-TimeSpan -Start $StartTime -End $endTime

    if($timeSpan.Days -lt 1)
    {
        $start = $StartTime
    }
    else
    {
        $start = $endTime
    }
    
    if($File.Length -eq 0)
	{
		$dateTimeString = Get-Date -Format "MM-dd-yyyyTHH-mm-ss"
		$File = "./vrtu-" + $dateTimeString + ".json"
	}
    
    if($BlobContainerName.Length -eq 0)
    {
		$BlobContainerName = "maps"
    }
    
    if($TableName.Length -eq 0)
    {
		$TableName = "gateway"
    }
    
    if($LogLevel.Length -eq 0)
    {
		$LogLevel = "Information"
    }
    
    if($LifetimeMinutes -eq 0)
    {
		$LifetimeMinutes = 525600
    }
    
    if($NodeCount -eq 0)
    {
		$NodeCount = 1
    }
    
    if($VmSize.Length -eq 0)
    {
		$VmSize = "Standard_D2s_v3"
    }
    
    if($VnetName.Length -eq 0)
    {
		$VnetName = "vrtu-vnet"
    }
    
    if($SubnetName.Length -eq 0)
    {
		$SubnetName = "vrtu-subnet"
    }
    
    if($VnetPrefix.Length -eq 0)
    {
		$VnetPrefix = "192.168.0.0/16"
    }
    
    if($SubnetPrefix.Length -eq 0)
    {
		$SubnetPrefix = "192.168.1.0/24"
    }
    
    if($ServiceCidr.Length -eq 0)
    {
		$ServiceCidr = "10.0.0.0/16"
    }
    
    if($DnsServiceIP.Length -eq 0)
    {
		$DnsServiceIP = "10.0.0.10"
    }
    
    if($PodCidr.Length -eq 0)
    {
		$PodCidr = "10.244.0.0/16"
    }
    
    if($DockerBridgeIP.Length -eq 0)
    {
		$DockerBridgeIP = "172.17.0.1/16"
    }
    
    #default values
    $claimTypes = "http://$Hostname/name"
    $claimValues = $VirtualRtuId
    $issuer = "http://$Hostname/"
    $audience = $issuer
    $filename = "$VirtualRtuId.json"
    
    Write-Host "LTM = $LifetimeMinutes" -ForegroundColor Cyan
    $ltm = '\"'+ $LifetimeMinutes + '\"'
    
    #get the service principal
    Update-Step -Step $step -Message "Set Service Principal" -Start $start
    $step++
    $spn = Get-ServicePrincipal -AppID $AppID -Password $Password   
    $spnAppId = $spn."appId"
    $spnPwd = $spn."pwd"
    
    $iotHubConnection = Get-IoTHubConnectionString -HubName "$IoTHubName" -ResourceGroupName "$ResourceGroupName"

	if($iotHubConnection.Length -eq 0)
	{
		Write-Host "No IoT Hub connection string...stopping script" -ForegroundColor DarkCyan
		return
	}
    
    #create the storage account
    $storageAvailable = Get-StorageAccountNameAvailable -SubscriptionName "$SubscriptionName" -StorageAcctName "$StorageAcctName"   
    if($storageAvailable)
    {
        New-StorageAccount -StorageAcctName $StorageAcctName -Location $Location -ResourceGroupName $ResourceGroupName       
    }

    $storageConnectionString = Get-StorageAccountConnectionString -StorageAcctName $StorageAcctName -ResourceGroupName $ResourceGroupName
    if($LASTEXITCODE -ne 0)
    {
        Write-Host "Failed to get VRTU storage account connection string...terminating script." -ForegroundColor Yellow
        return
    }


    Update-Step -Step $step -Message "VRTU storage account connection string obtained" -Start $start
    $step++ 

    #create app insights and get instrumentation key
    Update-Step -Step $step -Message "Creating App Insights for VRTU cluster and getting instrumentation key" -Start $start
    $step++
    $instrumentationKey = Get-InstrumentationKey "$VirtualRtuId" -ResourceGroupName $ResourceGroupName -Location $Location
    
    Update-Step -Step $step -Message "Creating new VRTU VNET" -Start $start
    $step++

    az network vnet create --resource-group $ResourceGroupName --name $VnetName --address-prefixes 192.168.0.0/16 --subnet-name $SubnetName --subnet-prefix 192.168.1.0/24

    $VnetName_ID=$(az network vnet show --resource-group $ResourceGroupName --name $VnetName --query id -o tsv)
    $SubnetName_ID=$(az network vnet subnet show --resource-group $ResourceGroupName --vnet-name $VnetName --name $SubnetName --query id -o tsv)

    Update-Step -Step $step -Message "Assigning role to service principal" -Start $start
    $step++
    az role assignment create --assignee $spnAppId --scope $VnetName_ID --role Contributor

    Update-Step -Step $step -Message "Creating new VRTU AKS cluster" -Start $start
    $step++
    az aks create --resource-group $ResourceGroupName --name $ClusterName --node-count $NodeCount --network-plugin kubenet --service-cidr $ServiceCidr --dns-service-ip $DnsServiceIP --pod-cidr $PodCidr --docker-bridge-address $DockerBridgeIP --vnet-subnet-id $SubnetName_ID --service-principal "$spnAppId" --client-secret "$spnPwd"    
	kubectl config use-context $ClusterName
	
    Update-Step -Step $step -Message "Getting AKS credentials" -Start $start
    $step++
    az aks get-credentials --resource-group $ResourceGroupName --name $ClusterName

    Update-Step -Step $step -Message "Applying Helm RBAC" -Start $start
    $step++
    kubectl apply -f "./helm-rbac.yaml"

    Update-Step -Step $step -Message "Starting Tiller" -Start $start
    $step++
    helm init --service-account tiller
    Set-Timer -Message "...waiting 45 seconds for Tiller to start" -Seconds 45

    Update-Step -Step $step -Message "Deploying helm chart for VRTU VNET" -Start $start
    $step++
    helm install "$Path/virtualrtu-vnet" --name virtualrtu --namespace kube-system --set claimTypes=$claimTypes --set claimValues=$claimValues --set issuer=$issuer --set audience=$audience --set lifetimeMinutes=$ltm --set symmetricKey=$SymmetricKey --set hostname=$Hostname --set storageConnectionString="$storageConnectionString" --set container=$BlobContainerName --set filename=$filename --set virtualRtuId=$VirtualRtuId --set instrumentationKey=$instrumentationKey --set logLevel=$LogLevel

    Write-Host "-- Step $step - Geting IP for Subnet communications" -ForegroundColor Green
    $ip = Get-ExternalIPForService -AppName "vrtu"
    $step++

    Write-Host "-- IP = $ip " -ForegroundColor Magenta
    
    Update-Step -Step $step -Message "Read file" -Start $start
    $step++
    if(Test-Path $File)
    {
		$config = Get-Content -Raw -Path $File | ConvertFrom-Json
		$config.claimValues = "$VirtualRtuId"
        $config.containerName = "$BlobContainerName"
        $config.filename = "$VirtualRtuId.json"
        $config.tableName = "$TableName"
        $config.lifetimeMinutes = $LifetimeMinutes
        $config.vrtuVmSize = "$VmSize"
        $config.virtualRtuId = "$VirtualRtuId"
        $config.vrtuConnectionString = "$storageConnectionString"
        $config.vrtuInstrumentationKey = "$instrumentationKey"
        $config.iotHubConnectionString = "$iotHubConnection"
        $config.vrtuIP = "$ip"
        
        Update-Step -Step $step -Message "Write file" -Start $start
		$step++
        $config | ConvertTo-Json -depth 100 | Out-File $File	
    }
    

    Write-Host "VRTU-VNET deployed!" -ForegroundColor Cyan
    
}

function Add-WedMonitorDeploy
{
	param([string]$Path, [string]$File, [string]$SubscriptionName, [string]$ResourceGroupName, [string]$PiraeusHostname,
    [string]$VirtualRtuId, [string]$StorageAcctName, [string]$SymmetricKey,
    [string]$Dns, [string]$Location, [string]$Email,
    [string]$ClusterName, [string]$Domain, [int]$Port, 
    [string]$AppID, [string]$Password, [string]$VmSize, [string]$TableName, [int]$NodeCount, [string]$LogLevel,
    [int]$Step, [DateTime]$StartTime)

    $env:AZURE_HTTP_USER_AGENT='pid-332e88b9-31d3-5070-af65-de3780ad5c8b'

    $step = 0
    $start = Get-Date

    if($LogLevel.Length -eq 0)
    {
		$LogLevel = "Information"
    }
    
    if($VmSize.Length -eq 0)
    {
		$VmSize = "Standard_D2s_v3"
    } 
    
    if($TableName.Length -eq 0)
    {
		$TableName = "gateway"
    }
    
    if($NodeCount -eq 0)
    {
		$NodeCount = 1
    }

	Update-Step -Step $step -Message "Set Subscription for deployment" -Start $start
	$step++
	Set-Subscription -SubscriptionName $SubscriptionName
	
	Update-Step -Step $step -Message "Set Resource Group for deployment" -Start $start
	$step++
	Set-ResourceGroup -ResourceGroupName $ResourceGroupName -Location $Location

    Update-Step -Step $step -Message "Get Service Principal" -Start $start
    $step++
    $spn = Get-ServicePrincipal -AppID $AppID -Password $Password   
    $spnAppId = $spn."appId"
    $spnPwd = $spn."pwd"   
    
    Update-Step -Step $step -Message "Get Tenant ID" -Start $start
    $step++
    $tenantId = Get-TenantId -SubscriptionName "$SubscriptionName"
    
    Update-Step -Step $step -Message "Check for App Registration" -Start $start
    $step++ 
    $appRegJson = az ad app list --display-name "$VirtualRtuId-Monitor" | ConvertFrom-Json 
    if($appRegJson.Length -eq 1)
    {
        Update-Step -Step $step -Message "Deleting App Registration" -Start $start
		$step++ 
		az ad app delete --id $appRegJson.appId
    }


    Update-Step -Step $step -Message "Register App in AAD and get Client Id" -Start $start
    $step++ 
    $appName = "$VirtualRtuId-Monitor"
    $replyUris = @("https://$Dns.$Location.cloudapp.azure.com/signin-oidc","https://locahost:44386")
    $clientId = New-RegisterApp -AppName $appName -ReplyUris $replyUris

	Update-Step -Step $step -Message "Get Storage Connection String" -Start $start
	$step++
	$vrtuConnectionString = Get-StorageAccountConnectionString -StorageAcctName "$StorageAcctName" -ResourceGroupName "$ResourceGroupName"

    Update-Step -Step $step -Message "See if Monitor AKS cluster exists" -Start $start
    $step++
    $clusterExists = Get-AksClusterExists -SubscriptionName $SubscriptionName -ClusterName $ClusterName

    if($clusterExists)
    {
        Update-Step -Step $step -Message "Setting kubectl context to monitor cluster" -Start $start
        $step++
        kubectl config use-context $ClusterName
        
        Update-Step -Step $step -Message "Updating ingress DNS and Location" -Start $start
        $step++
        #Update-MonitorIngressDns -Dns $Dns -Location $Location -Path "$Path/ingress-webmonitor.yaml" -Destination "$Path/ingress-webmonitor-$Dns.yaml"
	Set-Ingress -Dns $Dns -Location $Location -Path "$Path/ingress-webmonitor.yaml" -Destination "$Path/ingress-webmonitor-copy.yaml"
    }
    else
    {
        kubectl create namespace "webmon"
        Update-Step -Step $step -Message "Creating new Monitor AKS cluster" -Start $start
        $step++
        New-AksCluster -ClusterName $ClusterName -ResourceGroupName $ResourceGroupName -AppId "$spnAppId" -Password "$spnPwd" -VmSize $VmSize -NodeCount $NodeCount
	kubectl config use-context $ClusterName
		
        Update-Step -Step $step -Message "Get AKS credentials" -Start $start
        $step++
        Get-AksCredentials -ClusterName $ClusterName -ResourceGroupName $ResourceGroupName
		
        Update-Step -Step $step -Message "Apply HELM RBAC" -Start $start
        $step++
        New-KubectlApply -Filename "$Path/helm-rbac.yaml" -Namespace "kube-system"

        Update-Step -Step $step -Message "Start Tiller" -Start $start
        $step++
        helm init --service-account tiller
        Set-Timer -Message "...waiting 45 seconds for Tiller to start" -Seconds 45

        Update-Step -Step $step -Message "Add cert manager for Let's Encrypt" -Start $start
        $step++
        Add-CertManager2 -Namespace "cert-manager2"
        Set-Timer -Message "...waiting 45 seconds for cert-manager to initialize" -Seconds 45

        Update-Step -Step $step -Message "Add cert issuer for Lets Encrypt" -Start $start
        $step++
        Add-Issuer -Email $Email -IssuerPath "$Path/issuer2.yaml" -IssuerDestination "$Path/issuer2-copy.yaml" -Namespace "webmon"
        Set-Timer -Message "...waiting 30 seconds for issuer to initialize" -Seconds 30
   	
	Update-Step -Step $step -Message "Upate local HELM repo" -Start $start
	$step++
	helm repo add stable https://kubernetes-charts.storage.googleapis.com/
	
        Update-Step -Step $step -Message "Add NGINX" -Start $start
        $step++
        Add-NGINX -Namespace "webmon"
        Set-Timer "...waiting 45 seconds for nginx to initialize" -Seconds 45

        Update-Step -Step $step -Message "Get External IP address" -Start $start
        $step++
        $IP = Get-ExternalIP -Namespace "webmon"

        Update-Step -Step $step -Message "Create Public IP ID" -Start $start
        $step++
        $PUBLICIPID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$IP')].[id]" --output tsv)

        Update-Step -Step $step -Message "Update Public IP ID" -Start $start
        $step++
        Update-PublicIP -PublicIP $PUBLICIPID -Dns $Dns -SubscriptionName $SubscriptionName
     
        Update-Step -Step $step -Message "Set certificate for cert-manager and Lets Encrypt" -Start $start
        $step++
        Set-Certificate -Dns $Dns -Location $Location -Path "$Path/certificate2.yaml" -Destination "$Path/certificate2-copy.yaml" -Namespace "webmon"
       
	
    } 
    
    Update-Step -Step $step -Message "Creating App Insights for Monitor and getting instrumentation key" -Start $start
	$step++
	$aiKey = Get-InstrumentationKey "$Dns-monitors" -ResourceGroupName $ResourceGroupName -Location $Location

    Update-Step -Step $step -Message "Install Web Monitor from helm chart" -Start $start
    $step++
    $domain = "$Domain.onmicrosoft.com"
    $hostname = $PiraeusHostname 
    helm install "$Path/virtualrtu-webmonitor" --name virtualrtu-webmonitor --namespace "webmon" --set port=$Port --set symmetricKey=$SymmetricKey --set instrumentationKey=$aiKey --set logLevel=$LogLevel --set tenantId=$tenantId --set clientId=$clientId --set domain=$domain --set hostname=$hostname --set location=$Location --set storageConnectionString=$vrtuConnectionString --set tableName=$TableName
	if($LASTEXITCODE -ne 0 )
	{
        Update-Step -Step $step -Message "Waiting for Kubernetes API Services to start" -Start $start
        $step++
		Set-WaitForApiServices
        
        Update-Step -Step $step -Message "Trying again to install VRTU Monitor from helm chart" -Start $start
        $step++
		helm install "$Path/virtualrtu-webmonitor" --name virtualrtu-webmonitor --namespace "webmon" --set port=$Port --set symmetricKey=$SymmetricKey --set instrumentationKey=$aiKey --set logLevel=$LogLevel --set tenantId=$tenantId --set clientId=$clientId --set domain=$domain --set hostname=$hostname --set location=$Location --set storageConnectionString=$vrtuConnectionString --set tableName=$TableName
	}
	
   
    Set-Ingress -Dns $Dns -Location $Location -Path "$Path/ingress2.yaml" -Destination "$Path/ingress2-copy.yaml" -Namespace "webmon" 
}

function New-WebMonitorDeploy
{
    param([string]$Path, [string]$File, [string]$SubscriptionName, [string]$ResourceGroupName, [string]$PiraeusHostname,
    [string]$VirtualRtuId, [string]$StorageAcctName, [string]$SymmetricKey,
    [string]$Dns, [string]$Location, [string]$Email,
    [string]$ClusterName, [string]$Domain, [int]$Port, 
    [string]$AppID, [string]$Password, [string]$VmSize, [string]$TableName, [int]$NodeCount, [string]$LogLevel,
    [int]$Step, [DateTime]$StartTime)

    $step = $Step
    $step++

    $env:AZURE_HTTP_USER_AGENT='pid-332e88b9-31d3-5070-af65-de3780ad5c8b'
    
    $endTime = Get-Date
	$timeSpan = New-TimeSpan -Start $StartTime -End $endTime

    if($timeSpan.Days -lt 1)
    {
        $start = $StartTime
    }
    else
    {
        $start = $endTime
    }   
    
    if($LogLevel.Length -eq 0)
    {
		$LogLevel = "Information"
    }
    
    if($VmSize.Length -eq 0)
    {
		$VmSize = "Standard_D2s_v3"
    } 
    
    if($TableName.Length -eq 0)
    {
		$TableName = "gateway"
    }
    
    if($NodeCount -eq 0)
    {
		$NodeCount = 1
    }

	Update-Step -Step $step -Message "Set Subscription for deployment" -Start $start
	$step++
	Set-Subscription -SubscriptionName $SubscriptionName
	
	Update-Step -Step $step -Message "Set Resource Group for deployment" -Start $start
	$step++
	Set-ResourceGroup -ResourceGroupName $ResourceGroupName -Location $Location

    Update-Step -Step $step -Message "Get Service Principal" -Start $start
    $step++
    $spn = Get-ServicePrincipal -AppID $AppID -Password $Password   
    $spnAppId = $spn."appId"
    $spnPwd = $spn."pwd"   
    
    Update-Step -Step $step -Message "Get Tenant ID" -Start $start
    $step++
    $tenantId = Get-TenantId -SubscriptionName "$SubscriptionName"
    
    Update-Step -Step $step -Message "Check for App Registration" -Start $start
    $step++ 
    $appRegJson = az ad app list --display-name "$VirtualRtuId-Monitor" | ConvertFrom-Json 
    if($appRegJson.Length -eq 1)
    {
        Update-Step -Step $step -Message "Deleting App Registration" -Start $start
		$step++ 
		az ad app delete --id $appRegJson.appId
    }


    Update-Step -Step $step -Message "Register App in AAD and get Client Id" -Start $start
    $step++ 
    $appName = "$VirtualRtuId-Monitor"
    $replyUris = @("https://$Dns.$Location.cloudapp.azure.com/signin-oidc","https://locahost:44386")
    $clientId = New-RegisterApp -AppName $appName -ReplyUris $replyUris

	Update-Step -Step $step -Message "Get Storage Connection String" -Start $start
	$step++
	$vrtuConnectionString = Get-StorageAccountConnectionString -StorageAcctName "$StorageAcctName" -ResourceGroupName "$ResourceGroupName"

    Update-Step -Step $step -Message "See if Monitor AKS cluster exists" -Start $start
    $step++
    $clusterExists = Get-AksClusterExists -SubscriptionName $SubscriptionName -ClusterName $ClusterName

    if($clusterExists)
    {
        Update-Step -Step $step -Message "Setting kubectl context to monitor cluster" -Start $start
        $step++
        kubectl config use-context $ClusterName
        
        Update-Step -Step $step -Message "Updating ingress DNS and Location" -Start $start
        $step++
        #Update-MonitorIngressDns -Dns $Dns -Location $Location -Path "$Path/ingress-webmonitor.yaml" -Destination "$Path/ingress-webmonitor-$Dns.yaml"
	Set-Ingress -Dns $Dns -Location $Location -Path "$Path/ingress-webmonitor.yaml" -Destination "$Path/ingress-webmonitor-copy.yaml"
    }
    else
    {
        kubectl create namespace "webmon"
        Update-Step -Step $step -Message "Creating new Monitor AKS cluster" -Start $start
        $step++
        New-AksCluster -ClusterName $ClusterName -ResourceGroupName $ResourceGroupName -AppId "$spnAppId" -Password "$spnPwd" -VmSize $VmSize -NodeCount $NodeCount
	kubectl config use-context $ClusterName
		
        Update-Step -Step $step -Message "Get AKS credentials" -Start $start
        $step++
        Get-AksCredentials -ClusterName $ClusterName -ResourceGroupName $ResourceGroupName
		
        Update-Step -Step $step -Message "Apply HELM RBAC" -Start $start
        $step++
        New-KubectlApply -Filename "$Path/helm-rbac.yaml" -Namespace "kube-system"

        Update-Step -Step $step -Message "Start Tiller" -Start $start
        $step++
        helm init --service-account tiller
        Set-Timer -Message "...waiting 45 seconds for Tiller to start" -Seconds 45

        Update-Step -Step $step -Message "Add cert manager for Let's Encrypt" -Start $start
        $step++
        Add-CertManager2 -Namespace "cert-manager2"
        Set-Timer -Message "...waiting 45 seconds for cert-manager to initialize" -Seconds 45

        Update-Step -Step $step -Message "Add cert issuer for Lets Encrypt" -Start $start
        $step++
        Add-Issuer -Email $Email -IssuerPath "$Path/issuer2.yaml" -IssuerDestination "$Path/issuer2-copy.yaml" -Namespace "webmon"
        Set-Timer -Message "...waiting 30 seconds for issuer to initialize" -Seconds 30
   	
	Update-Step -Step $step -Message "Upate local HELM repo" -Start $start
	$step++
	helm repo add stable https://kubernetes-charts.storage.googleapis.com/
	
        Update-Step -Step $step -Message "Add NGINX" -Start $start
        $step++
        Add-NGINX -Namespace "webmon"
        Set-Timer "...waiting 45 seconds for nginx to initialize" -Seconds 45

        Update-Step -Step $step -Message "Get External IP address" -Start $start
        $step++
        $IP = Get-ExternalIP -Namespace "webmon"

        Update-Step -Step $step -Message "Create Public IP ID" -Start $start
        $step++
        $PUBLICIPID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$IP')].[id]" --output tsv)

        Update-Step -Step $step -Message "Update Public IP ID" -Start $start
        $step++
        Update-PublicIP -PublicIP $PUBLICIPID -Dns $Dns -SubscriptionName $SubscriptionName
     
        Update-Step -Step $step -Message "Set certificate for cert-manager and Lets Encrypt" -Start $start
        $step++
        Set-Certificate -Dns $Dns -Location $Location -Path "$Path/certificate2.yaml" -Destination "$Path/certificate2-copy.yaml" -Namespace "webmon"
       
	
    } 
    
    Update-Step -Step $step -Message "Creating App Insights for Monitor and getting instrumentation key" -Start $start
	$step++
	$aiKey = Get-InstrumentationKey "$Dns-monitors" -ResourceGroupName $ResourceGroupName -Location $Location

    Update-Step -Step $step -Message "Install Web Monitor from helm chart" -Start $start
    $step++
    $domain = "$Domain.onmicrosoft.com"
    $hostname = $PiraeusHostname 
    helm install "$Path/virtualrtu-webmonitor" --name virtualrtu-webmonitor --namespace "webmon" --set port=$Port --set symmetricKey=$SymmetricKey --set instrumentationKey=$aiKey --set logLevel=$LogLevel --set tenantId=$tenantId --set clientId=$clientId --set domain=$domain --set hostname=$hostname --set location=$Location --set storageConnectionString=$vrtuConnectionString --set tableName=$TableName
	if($LASTEXITCODE -ne 0 )
	{
        Update-Step -Step $step -Message "Waiting for Kubernetes API Services to start" -Start $start
        $step++
		Set-WaitForApiServices
        
        Update-Step -Step $step -Message "Trying again to install VRTU Monitor from helm chart" -Start $start
        $step++
		helm install "$Path/virtualrtu-webmonitor" --name virtualrtu-webmonitor --namespace "webmon" --set port=$Port --set symmetricKey=$SymmetricKey --set instrumentationKey=$aiKey --set logLevel=$LogLevel --set tenantId=$tenantId --set clientId=$clientId --set domain=$domain --set hostname=$hostname --set location=$Location --set storageConnectionString=$vrtuConnectionString --set tableName=$TableName
	}
	
   
    Set-Ingress -Dns $Dns -Location $Location -Path "$Path/ingress2.yaml" -Destination "$Path/ingress2-copy.yaml" -Namespace "webmon" 
  
    
    Update-Step -Step $step -Message "Read file" -Start $start
    $step++
    
    if(Test-Path $File)
    {
		$config = Get-Content -Raw -Path $File | ConvertFrom-Json
		$config.tenantId = "$tenantId"
        $config.clientId = "$clientId"
        $config.domain = "$domain"
        $config.monitorDns = "$Dns"
        $config.monitorPublicIP = "$IP"
        $config.monitorInstrumentationKey = "$aiKey"
        $config.monitorVmSize = "$VmSize"
        
        Update-Step -Step $step -Message "Write file" -Start $start
		$step++
        $config | ConvertTo-Json -depth 100 | Out-File $File	
    }  

}

function New-DeviceDeploy
{
  param ([string]$File, [string]$Url = "http://localhost:7071/api/DeploymentFunction", [string]$Key = $null, [bool]$Update = $false)

    $config = [System.IO.File]::ReadAllBytes($File)
    
    $requestUrl = $null
    
    if($Key -eq $null)
    {
		if($Update -eq $false)
		{
			$requestUrl = $Url
		}
		else
		{
			$requestUrl = $Url + "?type=update" 
		}
    }
    else
    {
		$requestUrl = $Url + "?code=" + $Key
		if($Update -ne $false)
		{
			$requestUrl = $requestUrl + "&type=update"
		}
    }
    
    if($Module -eq $null)
    {
		$connectionString = Invoke-WebRequest -Uri $requestUrl -Method Post -ContentType "application/json" -Body $config
		Write-Host $connectionString
    }
    else
    {
		Invoke-WebRequest -Uri $requestUrl -Method Post -ContentType "application/json" -Body $config
		Write-Host "Update complete"				
    }
}