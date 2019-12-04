function New-VrtuConfigFile()
{	
    param ([string]$Dns, [string]$VirtualRtuId, [string]$ClusterName, [string]$StorageAcctName, [string]$InFile, [string]$OutFile)
    
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
    }
   
    $output | ConvertTo-Json -depth 100 | Out-File $OutFile
}

