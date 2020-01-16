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