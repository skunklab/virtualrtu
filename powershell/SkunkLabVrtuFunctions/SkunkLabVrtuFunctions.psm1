function New-FunctionApp 
{
	param([string]$AppName, [string]$StorageAcctName,[string]$Location, [string]$AppInsightsName, [string]$ResourceGroupName)	

    	az functionapp create -n $AppName  --storage-account $StorageAcctName  --consumption-plan-location $Location  --app-insights $AppInsightsName --runtime dotnet -g $ResourceGroupName --functions-version 3
}

function Get-FunctionCreds
{
    param([string]$AppName, [string]$ResourceGroupName, [string]$SubscriptionName)

    $user = az webapp deployment list-publishing-profiles -n $AppName -g $ResourceGroupName --subscription $SubscriptionName --query "[?publishMethod=='MSDeploy'].userName" -o tsv
    $pass = az webapp deployment list-publishing-profiles -n $AppName -g $ResourceGroupName --subscription $SubscriptionName --query "[?publishMethod=='MSDeploy'].userPWD" -o tsv

    $pair = "$($user):$($pass)"
    $EncodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
    return $EncodedCreds
}

function Get-FunctionCode
{
    param([string]$AppName, [string]$FunctionName, [string]$EncodedCreds)

    $jwt = Invoke-RestMethod -Uri "https://$AppName.scm.azurewebsites.net/api/functions/admin/token" -Headers @{Authorization=("Basic {0}" -f $EncodedCreds)} -Method GET

    $keys = Invoke-RestMethod -Method GET -Headers @{Authorization=("Bearer {0}" -f $jwt)} `
            -Uri "https://$AppName.azurewebsites.net/admin/functions/$FunctionName/keys" 

    $code = $keys.keys[0].value
    return $code
}

function New-ConfigurationFunctionApp
{
	param([string]$Path, [string]$PublishFolder, [string]$ZipFilename, [string]$AppName, [string]$ResourceGroupName)
    
    Write-Host "Publish folder = $PublishFolder"
    $pathVal = Test-Path -Path $PublishFolder

    if(-Not $pathVal)
    {
        New-Item -Path $PublishFolder -ItemType directory
    }

	dotnet publish "$Path\..\src\VirtualRtu.Configuration.Function\VirtualRtu.Configuration.Function.csproj" -c Release -f netcoreapp3.1 -o "$PublishFolder" 
	
	# create the zip
	$publishZip = "$PublishFolder\$ZipFilename"
	if(Test-path $publishZip) {Remove-item $publishZip}
    Compress-Archive -Path "$PublishFolder\*" -DestinationPath $publishZip

	# deploy the zipped package
    az functionapp deployment source config-zip -g $ResourceGroupName -n $AppName --src "$publishZip"
}

function New-DeploymentFunctionApp
{
	param([string]$Path, [string]$PublishFolder, [string]$ZipFilename, [string]$AppName, [string]$ResourceGroupName)

	dotnet publish "$Path\..\src\AzureIoT.Deployment.Function\AzureIoT.Deployment.Function.csproj" -c Release -f netcoreapp3.1 -o "$PublishFolder" 
	

	# create the zip
	$publishZip = "$PublishFolder\$ZipFilename"
	if(Test-path $publishZip) {Remove-item $publishZip}
	Compress-Archive -Path "$PublishFolder\*" -DestinationPath $publishZip

	# deploy the zipped package
	az functionapp deployment source config-zip -g $ResourceGroupName -n $AppName --src $publishZip
}

function Update-ConfigSecrets
{
    param([string]$Path, [string]$Folder, [string]$SymmetricKey, [string]$ApiToken, [int]$LifetimeMinutes, [string]$TableName, [string]$StorageConnectionString, [string]$ContainerName, [string]$Filename)

    $config = Get-Content "$Path\$Folder\secrets.json" | ConvertFrom-Json
    $config.symmetricKey = $SymmetricKey
    $config.apiToken = $ApiToken
    $config.lifetimeMinutes = $LifetimeMinutes
    $config.tableName = $TableName
    $config.storageConnectionString = $StorageConnectionString
    $config.rtuMapContainer = $ContainerName
    $config.rtuMapFilename = $Filename

    $config | ConvertTo-Json -depth 100 | Out-File "$Path\$Folder\secrets.json"
}

function Update-DeploySecrets
{
    param([string]$Path, [string]$Folder, [string]$Hostname, [string]$ServiceUrl, [string]$TableName, [string]$StorageConnectionString, [string]$IoTHubConnectionString, [string]$Template)

    $config = Get-Content "$Path\$Folder\secrets.json" | ConvertFrom-Json
    $config.hostname = $Hostname    
    $config.tableName = $TableName
    $config.storageConnectionString = $StorageConnectionString
    $config.serviceUrl = $ServiceUrl
    $config.defaultIoTHubConnectionString = $IoTHubConnectionString
    $config.defaultTemplate = $Template

    $config | ConvertTo-Json -depth 100 | Out-File "$Path\$Folder\secrets.json"
}

function New-FileToBase64
{
    param([string]$Path, [string]$Filename)

    $fileBytes = [System.IO.File]::ReadAllBytes("$Path/$Filename")
    return [System.Convert]::ToBase64String($fileBytes)
}

function Get-FunctionAppExists
{
   param([string]$AppName, [string]$ResourceGroupName, [string]$SubscriptionName)
   $fqdn = "'$AppName.azurewebsites.net'"
   $fqdn
   $f = az functionapp list --resource-group $ResourceGroupName --subscription $SubscriptionName --query "[?defaultHostName==$fqdn].{hostName: defaultHostName}" | ConvertTo-Json

   if($f.Length -eq 4)
   {
      return $false
   }
   else
   {
      return $true
   }
}

function Remove-FunctionApp
{
    param([string]$AppName, [string]$ResourceGroupName, [string]$SubscriptionName)

    az functionapp delete --name $AppName --resource-group $ResourceGroupName --subscription $SubscriptionName
}

