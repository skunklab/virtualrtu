function New-VrtuVnetDeploy()
{
    param ([string]$File, [string]$VnetName = "vrtu-vnet", [string]$SubnetName = "vrtu-subnet")

    $vnet = $VnetName
    $subnet = $SubnetName

    $config = Get-Content -Raw -Path $File | ConvertFrom-Json
    
	$subscriptionNameOrId = $config.subscriptionNameOrId
	$resourceGroupName = $config.resourceGroupName
	$dnsName = $config.dnsName
	$location = $config.location
	$clusterName = $config.clusterName
	$piraeusHostname = $config.hostname
	$storageAcctName = $config.storageAcctName
	$containerName = $config.containerName
	$filename = $config.filename
	$appId = $config.appId
	$pwd = $config.pwd
	$nodeCount = $config.nodeCount
	$issuer = $config.issuer
	$audience = $config.audience
	$lifetimeMinutes = $config.lifetimeMinutes
	$symmetricKey = $config.symmetricKey
	$claimTypes = $config.claimTypes
	$claimValues = $config.claimValues
	$vmSize = $config.vmSize
	$virtualRtuId = $config.virtualRtuId
			
	$instrumentationKey= $config.instrumentationKey
	$logLevel = $config.logLevel
            
    $cleanup = Read-Host "Clean up previous kubectl deployment [y/n] ? "
    if($cleanup.ToLowerInvariant() -eq "y")
    {
        $cleanupClusterName = Read-Host "Enter previous cluster name [Enter blank == $clusterName] "
        $cleanupResourceGroup = Read-Host "Enter previous resource group name [Enter blank == $resourceGroupName] "
                
        if($cleanupClusterName.Length -eq 0)
        {
			$cleanupClusterName = $clusterName
        }
                
        if($cleanupResourceGroup.Length -eq 0)
        {
			$cleanupResourceGroup = $resourceGroupName
        }
                
        $condition1 = "users.clusterUser_" + $cleanupResourceGroup + "_" + $cleanupClusterName
        $condition2 = "clusters." + $cleanupClusterName
        kubectl config unset $condition1
        kubectl config unset $condition2
    }
            
    $env:AZURE_HTTP_USER_AGENT='pid-332e88b9-31d3-5070-af65-de3780ad5c8b'


    $saLine= az storage account check-name --name $storageAcctName
	if($saLine[2].Contains("true"))
	{
		az storage account create --location $location --name $storageAcctName --resource-group $resourceGroupName --sku "Standard_LRS"
	}
			
	$storageJsonString = az storage account show-connection-string --name $storageAcctName --resource-group $resourceGroupName
	$storageObj = ConvertFrom-Json -InputObject "$storageJsonString"
	$storageConnectionString = $storageObj.connectionString


    $step = 1
    #Set the subscription 
    Write-Host "-- Step $step - Setting subscription  $subscriptionNameOrId" -ForegroundColor Green
    az account set --subscription "$subscriptionNameOrId"
    if($LASTEXITCODE -ne 0)
    {
        Write-Host "Subscription - $subscriptionNameOrId could not be set" -ForegroundColor Yellow
        Write-Host "Exiting script" -ForegroundColor Yellow
        return;
    }
    $step++


    #Check if the Resource Group exists
    
    $resourceGroupNameoutcome = az group exists --name $resourceGroupName
            
    if($resourceGroupNameoutcome -eq "false")
    {
        Write-Host "Step $step - Create resource group '$resourceGroupName'" -ForegroundColor Green
        az group create --name $resourceGroupName --location $location 
        $step++
    }

    #Delete the AKS cluster if exists
    $clusterLine = az aks list --query "[?contains(name, '$clusterName')]" --output table

    if($clusterLine.Length -gt 0)
    {
        Write-Host "Step $step - Deleting old AKS cluster '$clusterName'" -ForegroundColor Green
        az aks delete --name $clusterName --resource-group $resourceGroupName --yes
        $step++
    }
            
    if($appId -eq $null -or $appId.Length -eq 0)
	{
		#create the service principal
		Write-Host "-- Step $step - Creating service principal" -ForegroundColor Green
		$creds = az ad sp create-for-rbac  --skip-assignment
		$credsObj = ConvertFrom-Json -InputObject "$creds"
		$appId = $credsObj.appId
		$pwd = $credsObj.password
	}

    Write-Host "Step $step - Create VNET $vnet" -ForegroundColor Green

            
    az network vnet create --resource-group $resourceGroupName --name $vnet --address-prefixes 192.168.0.0/16 --subnet-name $subnet --subnet-prefix 192.168.1.0/24

    $VNET_ID=$(az network vnet show --resource-group $resourceGroupName --name $vnet --query id -o tsv)
    $SUBNET_ID=$(az network vnet subnet show --resource-group $resourceGroupName --vnet-name $vnet --name $subnet --query id -o tsv)

    $step++

    Write-Host "Step $step - Assign SPN contributor role" -ForegroundColor Green
    az role assignment create --assignee $appId --scope $VNET_ID --role Contributor
    $step++

    Write-Host "Step $step - Create AKS cluster $clusterName" -ForegroundColor Green
    az aks create --resource-group $resourceGroupName --name $clusterName --node-count 1 --network-plugin kubenet --service-cidr 10.0.0.0/16 --dns-service-ip 10.0.0.10 --pod-cidr 10.244.0.0/16 --docker-bridge-address 172.17.0.1/16 --vnet-subnet-id $SUBNET_ID --service-principal $appId --client-secret $pwd
    $step++

    Write-Host "Step $step - Get AKS credentials" -ForegroundColor Green
    az aks get-credentials --resource-group $resourceGroupName --name $clusterName
    $step++

    Write-Host "Step $step - Apply Helm RBAC" -ForegroundColor Green
    kubectl apply -f "./helm-rbac.yaml"
    $step++

    Write-Host "-- Step $step - Intialize tiller" -ForegroundColor Green
    helm init --service-account tiller
    Write-Host "...waiting 45 seconds for Tiller to start" -ForegroundColor Yellow
    Start-Sleep -Seconds 45
    $step++

    Write-Host "-- Step $step - Deploying helm chart for vrtu-vnet" -ForegroundColor Green
	$lifetimeMinutes = '\"'+ $lifetimeMinutes + '\"'

    helm install ./virtualrtu-vnet --name virtualrtu --namespace kube-system --set claimTypes=$claimTypes --set claimValues=$claimValues --set issuer=$issuer --set audience=$audience --set lifetimeMinutes=$lifetimeMinutes --set symmetricKey=$symmetricKey --set hostname=$piraeusHostname --set storageConnectionString="$storageConnectionString" --set container=$containerName --set filename=$filename --set virtualRtuId=$virtualRtuId --set instrumentationKey=$instrumentationKey --set logLevel=$logLevel
    $step++

    Write-Host "-- Step $step - Geting IP for Subnet communications" -ForegroundColor Green
    $ip = GetExternalIP
    $step++

    Write-Host "-- IP = $ip " -ForegroundColor Magenta

    Write-Host "Done! Dare Mighty Things :-)" -ForegroundColor Cyan

}

function GetExternalIP()
{
    $looper = $TRUE
    while($looper)
    {   $externalIP = ""                  
        $lineValue = kubectl get service -l app=vrtu --namespace kube-system
        
        Write-Host "Last Exit Code for get external ip $LASTEXITCODE" -ForegroundColor White
        if($LASTEXITCODE -ne 0 )
        {
            Write-Host "Try get external ip...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }  
        elseif($lineValue.Length -gt 0)
        {
            $line = $lineValue[1]
            $lineout = $line -split '\s+'
            $externalIP = $lineout[3]
            #$lineParams = $lineValue.Replace("  "," ")
            #$lineParams = $lineParams.Replace("  "," ")
            #$lineItems = $lineParams.Split(" ")
            #$externalIP = $lineItems[4]      
        }
        
              
        if($externalIP -eq "<pending>")
        {        
            Write-Host "External IP is pending...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }
        elseif($externalIP.Length -eq 0)
        {
            Write-Host "External IP is zero length...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }
        else
        {
			$looper = $FALSE
            Write-Host "External IP is $externalIP" -ForegroundColor Magenta
            return $externalIP
        }
    }
}







az group create --name testdeploy --location eastus

$resourceGroupName = "testdeploy"
$vnet = "vrtu-vnet"
$subnet = "vrtu-subnet"


az network vnet create --resource-group $resourceGroupName --name $vnet --address-prefixes 192.168.0.0/16 --subnet-name $subnet --subnet-prefix 192.168.1.0/24

$VNET_ID=$(az network vnet show --resource-group $resourceGroupName --name $vnet --query id -o tsv)
$SUBNET_ID=$(az network vnet subnet show --resource-group $resourceGroupName --vnet-name $vnet --name $subnet --query id -o tsv)

$appId = "3112ca58-f443-4820-ba60-2e1ccc9a8a07"

az role assignment create --assignee $appId --scope $VNET_ID --role Contributor

$clusterName = "piraeusaks"
$pwd = "67e0239d-a660-4d79-b039-4ca7327bf8e7"
az aks create --resource-group $resourceGroupName --name $clusterName --node-count 1 --network-plugin kubenet --service-cidr 10.0.0.0/16 --dns-service-ip 10.0.0.10 --pod-cidr 10.244.0.0/16 --docker-bridge-address 172.17.0.1/16 --vnet-subnet-id $SUBNET_ID --service-principal $appId --client-secret $pwd

az aks get-credentials --resource-group $resourceGroupName --name $clusterName

kubectl apply -f "./helm-rbac.yaml"

helm init --service-account tiller

$config = Get-Content -Raw -Path "vrtu-config.json" | ConvertFrom-Json
		    
$subscriptionNameOrId = $config.subscriptionNameOrId
$resourceGroupName = $config.resourceGroupName
$dnsName = $config.dnsName
$location = $config.location
$clusterName = $config.clusterName
$piraeusHostname = $config.hostname
$storageAcctName = $config.storageAcctName
$containerName = $config.containerName
$filename = $config.filename
$appId = $config.appId
$pwd = $config.pwd
$nodeCount = $config.nodeCount
$issuer = $config.issuer
$audience = $config.audience
$lifetimeMinutes = $config.lifetimeMinutes
$symmetricKey = $config.symmetricKey
$claimTypes = $config.claimTypes


$lifetimeMinutes = '\"'+ $lifetimeMinutes + '\"'

helm install ./virtualrtu-vnet --name virtualrtu --namespace kube-system --set claimTypes=$claimTypes --set claimValues=$claimValues --set issuer=$issuer --set audience=$audience --set lifetimeMinutes=$lifetimeMinutes --set symmetricKey=$symmetricKey --set hostname=$piraeusHostname --set storageConnectionString="$storageConnectionString" --set container=$containerName --set filename=$filename --set virtualRtuId=$virtualRtuId --set instrumentationKey=$instrumentationKey --set logLevel=$logLevel
$claimValues = $config.claimValues
$vmSize = $config.vmSize
$virtualRtuId = $config.virtualRtuId
			
$instrumentationKey= $config.instrumentationKey
$logLevel = $config.logLevel