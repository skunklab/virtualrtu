function New-VrtuVnetDeploy()  
{
  param ([string]$File)

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

	$vnetName = "vrtuvnet"
    $subnetName = "vrtusubnet"


    
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
    $rgoutcome = az group exists --name $resourceGroupName
            
    if($rgoutcome -eq "false")
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

    Write-Host "Service Principal Application ID $appId" -ForegroundColor Cyan
    Write-Host "Service Principal Password $pwd" -ForegroundColor Cyan

	#create VNET
	Write-Host "-- Step $step Create the VNET"
	az network vnet create --resource-group $resourceGroupName --name $vnetName --address-prefixes 192.168.0.0/16 --subnet-name $subnetName --subnet-prefix 192.168.1.0/24
	$step++
	
	$subnetId=$(az network vnet subnet show -g $resourceGroupName -n $subnetName --vnet-name $vnetName --query id -o tsv)
	
	Write-Host "-- Step $step Assign SPN contributor permission"
	az role assignment create --assignee $appId --scope $vnetName --role Contributor --subscription $subscriptionNameOrId 
	$step++	


    #create AKS cluster in VNET
    Write-Host "-- Step $step - Create AKS cluster in VNET" -ForegroundColor Green
    az aks create --name $clusterName --resource-group $resourceGroupName --node-count 1 -s $vmSize -k 1.13.9 --service-principal $appId --client-secret $pwd --vnet-subnet-id $subnetId --network-plugin kubenet --enable-addons monitoring --service-cidr 192.168.0.0/16 --dns-service-ip 192.168.0.10 --pod-cidr 10.244.0.0/16 --docker-bridge-address 172.17.0.1/16 -node-vm-size $vmSize
    $step++

    Write-Host "-- Step $step - Get AKS credentials" -ForegroundColor Green
    GetAksCredentials $resourceGroupName $clusterName --overwrite-existing
    $step++
    
            
    #apply RBAC
    Write-Host "-- Step $step - Apply kubectl RBAC" -ForegroundColor Green
    ApplyYaml "./helm-rbac.yaml"
    $step++

    #initialize tiller with helm
    Write-Host "-- Step $step - Intialize tiller" -ForegroundColor Green
    helm init --service-account tiller
    Write-Host "...waiting 45 seconds for Tiller to start" -ForegroundColor Yellow
    Start-Sleep -Seconds 45
    $step++


    #deploy the helm chart
    #apply the virtual rtu helm chart
    Write-Host "-- Step $step - Deploying helm chart for vrtu in vnet" -ForegroundColor Green
	$lifetimeMinutes = '\"'+ $lifetimeMinutes + '\"'

    helm install ./virtualrtu-vnet --name virtualrtu-vnet --namespace kube-system --set claimTypes=$claimTypes --set claimValues=$claimValues --set issuer=$issuer --set audience=$audience --set lifetimeMinutes=$lifetimeMinutes --set symmetricKey=$symmetricKey --set piraeusHostname=$piraeusHostname --set storageConnectionString="$storageConnectionString" --set containerName=$containerName --set filename=$filename
    $step++

    Write-Host "Done!  Dare Mighty Things :-)" -ForegroundColor Cyan
  
}


function GetAksCredentials()
{
    param([string]$rgn, [string]$cn)

    $looper = $true
    while($looper)
    {
        try
        {         
            az aks get-credentials --resource-group $rgn --name $cn
            $looper = $false
        }
        catch
        {
            Write-Host "Waiting 30 seconds to try get aks credentials again..." -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }    
    }
}