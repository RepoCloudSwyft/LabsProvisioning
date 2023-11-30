# Labs Provisioning API
It contains different api for provisioning CLMP LABS

1. [**InvokeNewTenant**](#invoke-new-tenant) - It creates resource group with Virtual Network (VNET) and Network Security Group (NSG) under the selected subscription.
2. [**InvokeNewTenantWithFW**](#invoke-new-tenant) - It creates resource group with Firewall setup under the selected subscription
3. [**LabsProvision2_2_1**](#labs-provisioning) - It is a v2.2.1 labs provisioning and it creates Virtual Machine (VM), Network Interface (NIC) and Public IP (IP).
4. [**LabsProvisionWithFW**](#labs-provisioning) - It is also a labs provisioning with Firewall setup.
5. [**LabsProvisionWithFWLimit**](#labs-provisioning) - It is also a labs provisioning with Firewall setup ip limit.

# How to setup
1. Clone the [repository](https://github.com/RepoCloudSwyft/LabsProvisioning.git)
2. Open the LabsProvisioning.sln in Visual Studio
3. Create *local.settings.json*
4. Paste the following code and change the value

``` json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "DAILY_REPORT_CRON_TIME": "0 0 1 * * *",
    "CRON_TIME": "0 0 0,3 * * *",
    "WEBSITE_TIME_ZONE": "Singapore Standard Time",
    "CreateEnvironmentVariablesPsUrl": "https://raw.githubusercontent.com/onecliquezone/create-sysenv/main/create-environment-variables-windows.ps1",
    "SendGridKey": "SG.xxxxxxxxxxxx",
    "SendGridName": "apikey",
    "TO": "[\"email@domain.com\"]",
    "CC": "[\"email@domain.com\",\"email@domain.com\",\"email@domain.com\"]",
    "FROM": "email@domain.com",
    "FROM_DAILYREPORT_EMAIL": "email@domain.com"
  }
}
```

| Parameters                      | Content                 | Description                   |
| :---                            | :----                   | :---                          |
| DAILY_REPORT_CRON_TIME          | 0 0 1 * * *             | Azure Function App Cron Time  |
| CRON_TIME                       | 0 0 0,3 * * *           | Azure Function App Cron Time  |
| [WEBSITE_TIME_ZONE](https://learn.microsoft.com/en-us/azure/azure-functions/functions-app-settings#website_time_zone)               | Singapore Standard Time | Azure Function App time zone |
| CreateEnvironmentVariablesPsUrl | https://raw.githubusercontent.com/onecliquezone/create-sysenv/main/create-environment-variables-windows.ps1  | Powershell script for creating environment variables in windows         |
| SendGridKey                     | SG.xxxxxxxxxxxx         | Send Grid key credential for sending email  |
| SendGridName                    | apikey                  | Send Grid name credential for sending email |
| TO                              | [\"email@domain.com\"]  | Array of recipient email  |
| CC                              | [\"email@domain.com\",\"email@domain.com\",\"email@domain.com\"]  | Array of recipient email for cc |
| FROM                            | email@domain.com        | Force shutdown report sender email         |
| FROM_DAILYREPORT_EMAIL          | email@domain.com        | Daily report sender email         |

5. Wait for the NuGet package to install, if it shows an error on auto installation, you can install the NuGet manually. 
6. Run the program



# How to use
## Invoke New Tenant
InvokeNewTenant and InvokeNewTenantWithFW uses same request body, only the provisioned resources is the difference between the two.



| API                   | Has Firewall  |
| ---                   | :---:         |
| InvokeNewTenant       | False         |
| InvokeNewTenantWithFW | True          |

``` json
{
    "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "applicationKey": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "applicationId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "clientCode": "ABCDE",
    "environment": "P",
    "contactEmail": "johnpatrickpapares@gmail.com",
    "location": "East Asia"
}
```
| Parameters        | Content                               | Description                   |
| :---              | :----:                                | :---                          |
| subscriptionId    | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Azure Subscription ID         |
| tenantId          | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Azure Tenant ID               |
| applicationKey    | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Application Registration Key  |
| applicationId     | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Application Registration ID   |
| clientCode        | ABCDE                                 | Client name (5 letters)       |
| environment       | P                                     | Deployment environment: S, P, D, U, Q |
| contactEmail      | johnpatrickpapares@gmail.com          | Contact email                 |
| location          | East Asia                             | Azure valid location          |



## Labs Provisioning
The following types of labs provisioning uses same request body, but it create different resources and configuration 


| API                       | OS        | Supported |
| ---                       | ---       | :---:     |
| LabsProvision2_2_1        | Windows   | True      |
| LabsProvision2_2_1        | Linux     | True      |
| LabsProvisionWithFW       | Windows   | True      |
| LabsProvisionWithFW       | Linux     | False     |
| LabsProvisionWithFWLimit  | Windows   | True      |
| LabsProvisionWithFWLimit  | Linux     | False     |

``` json
{
  "SubscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ApplicationKey": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ApplicationId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "Location": "East Asia",
  "Environment": "D",
  "ClientCode": "ABCDE",
  "VirtualMachineName": "CS-ABCDE-D-VM-U40V3-15885o8912344d25b58G0ba611e8555b",
  "Size": "Standard_B2ms",
  "IsCustomTemplate": false,
  "TempStorageSizeInGb": 127,
  "ImageUri": "https://devvmdisk.blob.core.windows.net/virtual-machine-image-templates/TEMPLATE.Vhd",
  "ContactPerson": "j-patrick@cloudswyft.com",
  "StorageAccountName": "devvmdisk",
  "OsType": "Windows",
  "ComputerName": "hmt4bxtdm0",
  "Username": "hmt4bxtdm0",
  "Password": "MV6pDe7GWkgO!",
  "Fqdn": "labs.domain.com/",
  "apiprefix": "dev",
  "uniqueId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```
| Parameters            | Content                               | Description                   |
| :---                  | :----                                 | :---                          |
| SubscriptionId        | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Azure Subscription ID         |
| TenantId              | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Azure Tenant ID               |
| ApplicationKey        | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Application Registration Key  |
| ApplicationId         | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Application Registration ID   |
| Location              | East Asia                             | Azure valid location          |
| Environment           | P                                     | Deployment environment: S, P, D, U, Q |
| ClientCode            | ABCDE                                 | Client name (5 letters)       |
| VirtualMachineName    | CS-ABCDE-D-VM-U40V3-15885o8912344...  | Azure Virtual Machine Name    |
| Size                  | Standard_B2ms                         | Azure valid VM Size           |
| IsCustomTemplate      | false                                 | True if custom template, false if not |
| TempStorageSizeInGb   | 127                                   | Valid size in GB              |
| ImageUri              | https://devvmdisk.blob.core.windows.net/vhds/TEMPLATE.Vhd  | Azure valid vhd uri |
| ContactPerson         | johnpatrickpapares@gmail.com          | Contact person                        |
| StorageAccountName    | devvmdisk                             | Existing Storage account              |
| OsType                | WINDOWS                               | Supported OS only: WINDOWS or LINUX   |
| ComputerName          | hmt4bxtdm0                            | Auto-generated or customized name     |
| Username              | hmt4bxtdm0                            | Auto-generated or customized name     |
| Password              | MV6pDe7GWkgO!                         | Auto-generated or customized password |
| Fqdn                  | labs.domain.com/                      | Cloud Labs target domain              |
| apiprefix             | dev                                   | Folder location inside clmp web app   |
| uniqueId              | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  | Auto-generated unique id              |

