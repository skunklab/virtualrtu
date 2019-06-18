After deploying the Azure Function
(1) Stop the function
(2) Add the Environment Variables (below)
(3) Restart the function
  
  NAME									DESCRIPTION
  _________________________________________________________________________
	
  FUNC_StorageConnectionString  |  The connection string for the Azure storage account where RTU maps are stored and the LUSS table.
  FUNC_PiraeusHostname          |  The hostname for Piraeus, e.g., ptest1.eastus.cloudapp.azure.com
  FUNC_NameClaimType            |  The claim type the uniquely identifies a user in the security token, e.g., http://skunklab.io/name
  FUNC_RoleClaimType            |  The claim type the identifies a role in the security token, e.g., http://skunklab.io/role
  FUNC_SymmetricKey             |  A base64 encoded 256-bit (32-byte) symmetric key used to sign and verify Piraeus security tokens used to connect to a gateway.
  FUNC_LifetimeMinutes          |  The number of minutes, e.g., 1 year = 252600, that the security token is valid for a client.
  FUNC_Issuer                   |  The issuer of the security token, e.g., http://skunklab.io/
  FUNC_Audience                 |  The audience of the security token, e.g., http://skunklab.io/
  FUNC_PiraeusApiToken          |  The Piraeus Management API Token/Key used to obtain a security to call the API.
  FUNC_ContainerName            |  The container name in Azure Blob storage where RTU maps reside, e.g., "maps".
  FUNC_Filename                 |  The filename of the RTU map that this function will update, e.g., "rtumap.json".
  FUNC_TableName                |  The Azure storage table name, e.g., "lusstokens", that the function uses to manage provisioning.

   
