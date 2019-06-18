---
title: Azure VRTU Provisioning Function
---

Introduction
============

The purpose of the Azure VRTU provisioning function is to orchestrate the
provisioning process and enable the VRTU, VRTU field gateway, and SCADA client
to communicate with the physical RTU. The Azure function performs the following:

1.  Authenticate the LUSS sent by the VRTU field gateway

2.  Collect information provided by the Azure VRTU provisioning app service

3.  Provision CAPL authorization policies in Piraeus the shared π-systems from
    the VRTU field gateway and VRTU.

4.  Provision the shared π-systems and reference the CAPL authorization polices

5.  Provide a security token to the VRTU field gateway that binds it to the
    provisioning π-systems

6.  Updates the RTU map that is read by the VRTU

System Resources Used
=====================

The Azure VRTU provisioning app service inputs information into an Azure storage
table, including the LUSS and its expiry. The Function must be able to read and
update the table order to authenticate the LUSS and ensure it can only be used
1-time. The additional information in the table is used to create the π-systems
for communications, authorization policies, and update the RTU map used by the
VRTU. The RTU map tells the VRTU which Unit IDs (which map to physical RTUs) are
associated with the π-systems used to communicate. The RTU map is stored in
Azure Blob storage. The output of the Function is configuration information sent
to the VRTU field gateway. This configuration information includes the π-systems
used for communications by the VRTU field gateway, and a security token that
authenticates with the Piraeus gateway and can only be used to access the
π-systems for that field gateway’s communications. The Function must call the
Piraeus Management API to create the authorization policies and π-systems used
for communications.

Function Configuration Information
==================================

The Azure Function should be deployed, then configure by adding these
Environment Variables. Once the Environment variables are added the function
should be restarted.

| Environment Variable         | Description                                                                                                      |
|------------------------------|------------------------------------------------------------------------------------------------------------------|
| FUNC_Issuer                  | The issuer of the security token, e.g., “https://skunklab.io/”                                                   |
| FUNC_Audience                | The audience of the security token, e.g., “https://skunklab.io/”                                                 |
| FUNC_LifetimeMinutes         | The number of minutes the security token is valid, i.e., 525600 is 1-year.                                       |
| FUNC_SymmetricKey            | The 256-bit/32-byte base64 encoded symmetric key used to sign and validate the security token.                   |
| FUNC_NameClaimType           | The claim type of the name claim that uniquely identities the VRTU field gateway, e.g., http://skunklab.io/name  |
| FUNC_RoleClaimType           | The claim type of the role claim of the VRTU field gateway, e.g., http://skunklab.io/role                        |
| FUNC_PiraeusApiToken         | The Piraeus Management API code used to obtain a security token to use the API.                                  |
| FUNC_StorageConnectionString | The storage connection string where the LUSS table exists and where the RTU map can be accessed in Blob storage. |
| FUNC_ContainerName           | The container name where the RTU map is stored, e.g., “maps”.                                                    |
| FUNC_Filename                | The filename of the RTU map in the container, e.g., “rtumap.json”                                                |
| FUNC_TableName               | The name of the table in Azure storage that contains the LUSS information.                                       |
