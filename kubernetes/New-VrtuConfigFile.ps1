function New-VrtuConfigFile()
{	
    param ([string]$Dns, [string]$VirtualRtuId, [string]$ClusterName, [string]$StorageAcctName, [string]$IoTHubName, [string]$InFile, [string]$OutFile)
    
    if($Dns.Length -eq 0)
    {
		Write-Host "Dns name is omitted...exiting script" -ForegroundColor Yellow
		return
    }
    
    if($ClusterName.Length -eq 0)
    {
        $ClusterName = "vrtucluster"
    }


    $config = Get-Content -Raw -Path $InFile | ConvertFrom-Json
    $hostname = $config.dnsName + "." + $config.location + ".cloudapp.azure.com"
    $claimTypes = "http://" + $hostname + "/name"
    $filename = $VirtualRtuId + ".json"
	$apiTokens = $config.apiSecurityCodes
	$apiCode = $apiTokens.Split(";")[0]	
    
    $aiKey = GetInstrumentationKey "$Dns-vtrus" $config.resourceGroupName $config.location
    Write-Host "Instrumenetation Key $aiKey"
    $connectionString = GetIoTHubConnectionString $IoTHubName $config.resourceGroupName
    
    
    $output = [PSCustomObject]@{
    subscriptionNameOrId = $config.subscriptionNameOrId
    resourceGroupName = $config.resourceGroupName
    virtualRtuId = $VirtualRtuId
    clusterName = $ClusterName
    dnsName = $Dns
    location = $config.location
    storageAcctName = $StorageAcctName   
    hostname = $hostname
    nodeCount = 1
    vmSize = "Standard_D2s_v3"
    appId = $config.appId
    pwd = $config.pwd
    issuer = $config.issuer
    audience = $config.audience
    symmetricKey = $config.symmetricKey
    apiToken = $apiCode
    lifetimeMinutes = 525600
    claimTypes = $claimTypes
    claimValues = $VirtualRtuId
    containerName = "maps"
    filename = $filename
    logLevel = $config.logLevel
    instrumentationKey = $aiKey
    iotHubConnectionString = $connectionString
    }   
    $output | ConvertTo-Json -depth 100 | Out-File $OutFile
    
    Write-Host "Done :-)" -ForegroundColor Cyan
}

function GetInstrumentationKey()
{
    param([string]$appName, [string]$rg, [string]$loc)

    $jsonAppString = az monitor app-insights component show --app $appName -g $rg
	$host.ui.RawUI.ForegroundColor = 'Gray'	
    if($LASTEXITCODE -ne 0)
    {
	    Write-Host "-- Step $step - Creating App Insights $appName" -ForegroundColor Green
			
	    $jsonAppString = az monitor app-insights component create -a $appName -l $loc -k other -g $rg --application-type other
	    $step++       
    }

    $appKey = NewRandomKey(8)
    $jsonKeyString = az monitor app-insights api-key create --api-key $appKey -g testdeploy -a $appName
    $keyObj = ConvertFrom-Json -InputObject "$jsonKeyString"
    $iKey = $keyObj.apiKey	
	$host.ui.RawUI.ForegroundColor = 'Gray'
	Write-Host "App Insights created" -ForegroundColor Green
    return $iKey
}

function NewRandomKey()	   
{
    param([int]$Length)
    
	$random = new-Object System.Random
	$buffer = [System.Byte[]]::new($Length)
	$random.NextBytes($buffer)
	$stringVar = [Convert]::ToBase64String($buffer)
    if($stringVar.Contains("+") -or $stringVar.Contains("/"))
    {
        return NewRandomKey($Length)
    }
    else
    {
        return $stringVar
    }
}

function GetIoTHubConnectionString()
{
    param([string]$hubName, [string]$rg)
        
    $hubJsonString = az iot hub show-connection-string --name $hubName --resource-group $rg

    if($LASTEXITCODE -ne 0)
    {
		$host.ui.RawUI.ForegroundColor = 'Gray'
        Write-Host "IoT Hub not exists.  Try to create a free IoT Hub"
        $res = az iot hub create --name $hubName --resource-group $rg --sku F1
        $host.ui.RawUI.ForegroundColor = 'Gray'
        if($LASTEXITCODE -ne 0)
        {
			$host.ui.RawUI.ForegroundColor = 'Gray'
			Write-Host "Cannot create a free IoT Hub (F1) because it is already used." -ForegroundColor Yellow
            $newS1Sku = Read-Host "Would you like to create an S1 SKU (25/month) [y/n] ? "
			if($newS1Sku.ToLowerInvariant() -eq "y")
			{
				$host.ui.RawUI.ForegroundColor = 'Gray'
				az iot hub create --name $hubName --resource-group $rg --sku S1
            }
            else
            {
				Write-Host("Exiting script") -ForegroundColor Yellow
				return
            }
        }
        
        Write-Host "IoT Hub created" -ForegroundColor Green       
    }

	Start-Sleep -Seconds 60
	$host.ui.RawUI.ForegroundColor = 'Gray'
    $hubJsonString2 = az iot hub show-connection-string --name $hubName --resource-group $rg
	
    $hubObj = ConvertFrom-Json -InputObject "$hubJsonString2"
    $cs = $hubObj.connectionString
    return $cs
}