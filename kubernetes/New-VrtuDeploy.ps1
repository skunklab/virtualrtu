function New-VrtuDeploy()
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
			

            #Remove previous deployments from kubectl
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
            
            #create AKS cluster
            Write-Host "-- Step $step - Create AKS cluster" -ForegroundColor Green
            az aks create --resource-group $resourceGroupName --name $clusterName --node-count $nodeCount --service-principal $appId --client-secret $pwd --node-vm-size $vmSize 
            $step++
            
            #get AKS credentials
            Write-Host "-- Step $step - Get AKS credentials" -ForegroundColor Green
            GetAksCredentials $resourceGroupName $clusterName
            #az aks get-credentials --resource-group $resourceGroupName --name $clusterName
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

            #apply the virtual rtu helm chart
            Write-Host "-- Step $step - Deploying helm chart for vrtu" -ForegroundColor Green
	        $lifetimeMinutes = '\"'+ $lifetimeMinutes + '\"'
			
			Write-Host "ClaimTypes = $claimTypes"
			Write-Host "ClaimValues = $claimValues"
			Write-Host "Issuer = $issuer"
			Write-Host "Audience = $audience"
			Write-Host "LifetimeMinutes = $lifetimeMinutes"
			Write-Host "SymmetricKey = $symmetricKey"
			Write-Host "PiraeusHostname = $piraeusHostname"
			Write-Host "StorageConnectionString = $storageConnectionString"
			Write-Host "ContainerName = $containerName"
			Write-Host "Filename = $filename"
			
			
			
            helm install ./virtualrtu --name virtualrtu --namespace kube-system --set claimTypes=$claimTypes --set claimValues=$claimValues --set issuer=$issuer --set audience=$audience --set lifetimeMinutes=$lifetimeMinutes --set symmetricKey=$symmetricKey --set piraeusHostname=$piraeusHostname --set storageConnectionString="$storageConnectionString" --set containerName=$containerName --set filename=$filename
            $step++

            #kubectl expose deployment virtualrtu-iotedge-vrtu-vrtu --type=LoadBalancer --name=vrtu-service --namespace kube-system
            
            Write-Host "-- Step $step - VRTU's external IP" -ForegroundColor Green
            $IP = GetExternalIP            
            Write-Host "Got external IP = $IP" -ForegroundColor Yellow
         
            # Get the resource-id of the public ip
            $PUBLICIPID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$IP')].[id]" --output tsv)
            Write-Host "PublicIPID = $PUBLICIPID" -ForegroundColor Yellow
            $step++

            #update the azure network with the public IP ID
            Write-Host "-- Step $step - Update Azure Network with Public IP ID" -ForegroundColor Green
            if($subscriptionNameOrId.Length -ne 0)
            {
              az network public-ip update --ids $PUBLICIPID --dns-name $dnsName --subscription $subscriptionNameOrId
            }
            else
            {
              az network public-ip update --ids $PUBLICIPID --dns-name $dnsName
            }
            $step++

            Write-Host "External IP to VRTU $IP" -ForegroundColor Magenta
            Write-Host "--- Done :-) Dare Mighty Things ---" -ForegroundColor Cyan

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



function ApplyYaml
{
    param([string]$file)

    $looper = $true
    while($looper)
    {
        kubectl apply -f $file
        if($LASTEXITCODE -ne 0)
        {
            Write-Host "kubectl apply failed for $file. Waiting 10 seconds to try again..." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
        }
        else
        {
            $looper = $false
        }
    }
}

function UpdateYaml()
{
    Param ([string]$newValue, [string]$matchString, [string]$filename)

    (Get-Content $filename) -replace $matchString,$newValue | out-file $filename -Encoding ascii
}

#---- end functions
			
