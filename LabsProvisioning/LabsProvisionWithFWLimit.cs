using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using LabsProvisioning.Model;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Network.Fluent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Net;
using Microsoft.Azure.Management.Compute.Fluent;

namespace LabsProvisioning
{
    public static class LabsProvisionWithFWLimit
    {

        [FunctionName("LabsProvisionWithFWLimit")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Start of Provisioning");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            LabsProvisionModel labsProvision = JsonConvert.DeserializeObject<LabsProvisionModel>(requestBody);

            if (string.IsNullOrEmpty(labsProvision.SubscriptionId) ||
                string.IsNullOrEmpty(labsProvision.TenantId) ||
                string.IsNullOrEmpty(labsProvision.ApplicationId) ||
                string.IsNullOrEmpty(labsProvision.ApplicationKey) ||
                string.IsNullOrEmpty(labsProvision.Location) ||
                string.IsNullOrEmpty(labsProvision.Environment) ||
                string.IsNullOrEmpty(labsProvision.ClientCode) ||
                string.IsNullOrEmpty(labsProvision.VirtualMachineName) ||
                string.IsNullOrEmpty(labsProvision.Size) ||
                //string.IsNullOrEmpty(labsProvision.ImageUri) ||
                string.IsNullOrEmpty(labsProvision.ContactPerson) ||
                string.IsNullOrEmpty(labsProvision.StorageAccountName) ||
                string.IsNullOrEmpty(labsProvision.OsType) ||
                string.IsNullOrEmpty(labsProvision.ComputerName) ||
                string.IsNullOrEmpty(labsProvision.Username) ||
                string.IsNullOrEmpty(labsProvision.Password) ||
                string.IsNullOrEmpty(labsProvision.uniqueId))
            {
                log.LogInformation("Incorect Request Body.");

                return new BadRequestObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        message = "Incorect Request Body.",
                        requestBody = JsonConvert.SerializeObject(labsProvision)
                    })
                );
            }

            if (labsProvision.IsManaged)
            {
                if (string.IsNullOrEmpty(labsProvision.imageReferenceId))
                {
                    log.LogInformation("Incorect Request Body.");

                    return new BadRequestObjectResult(
                        JsonConvert.SerializeObject(new
                        {
                            message = "Incorect Request Body.",
                            requestBody = JsonConvert.SerializeObject(labsProvision)
                        })
                    );
                }
            }
            else
            {
                if (string.IsNullOrEmpty(labsProvision.ImageUri))
                {
                    log.LogInformation("Incorect Request Body.");

                    return new BadRequestObjectResult(
                        JsonConvert.SerializeObject(new
                        {
                            message = "Incorect Request Body.",
                            requestBody = JsonConvert.SerializeObject(labsProvision)
                        })
                    );
                }
            }


            string subscriptionId = labsProvision.SubscriptionId;
            string tenantId = labsProvision.TenantId;
            string applicationId = labsProvision.ApplicationId;
            string applicationKey = labsProvision.ApplicationKey;

            string environment = labsProvision.Environment;
            string clientCode = labsProvision.ClientCode;

            string location = labsProvision.Location;
            string virtualMachineName = labsProvision.VirtualMachineName.ToUpper();
            string size = labsProvision.Size;
            int tempStorageSizeInGb = labsProvision.TempStorageSizeInGb;
            string imageUri = labsProvision.ImageUri;
            string imageReferenceId = labsProvision.imageReferenceId;
            string contactPerson = labsProvision.ContactPerson;
            string storageAccountName = labsProvision.StorageAccountName;
            string osType = labsProvision.OsType;
            string computerName = labsProvision.ComputerName;
            string username = labsProvision.Username;
            string password = labsProvision.Password;

            string fqdn = labsProvision.Fqdn;
            string apiprefix = labsProvision.apiprefix;

            bool isTenant = labsProvision.IsCustomTemplate;
            bool deallocateWhenFinish = labsProvision.DeallocateWhenFinish;

            string createEnvironmentVariablesPsUrl = 
                ResourceHelper.GetEnvironmentVariable("CreateEnvironmentVariablesPsUrl") != null ? 
                ResourceHelper.GetEnvironmentVariable("CreateEnvironmentVariablesPsUrl") : 
                "https://raw.githubusercontent.com/onecliquezone/create-sysenv/main/create-environment-variables-windows.ps1";
            log.LogInformation(createEnvironmentVariablesPsUrl);
            
            bool isVersion2_2 = string.IsNullOrEmpty(labsProvision.ResourceGroupName);

            bool isManaged = labsProvision.IsManaged;

            try
            {
                ServicePrincipalLoginInformation principalLogIn = new ServicePrincipalLoginInformation();
                principalLogIn.ClientId = applicationId;
                principalLogIn.ClientSecret = applicationKey;

                AzureEnvironment azureEnvironment = AzureEnvironment.AzureGlobalCloud;
                AzureCredentials credentials = new AzureCredentials(principalLogIn, tenantId, azureEnvironment);

                string uniqueId = labsProvision.uniqueId; // Guid.NewGuid().ToString().Replace("-", "");

                log.LogInformation($"subscriptionId: {subscriptionId}");
                log.LogInformation($"tenantId: {tenantId}");
                log.LogInformation($"applicationId: {applicationId}");
                log.LogInformation($"environment: {environment}");
                log.LogInformation($"clientCode: {clientCode}");
                log.LogInformation($"location: {location}");
                log.LogInformation($"virtualMachineName: {virtualMachineName}");
                log.LogInformation($"size: {size}");
                log.LogInformation($"tempStorageSizeInGb: {tempStorageSizeInGb}");
                log.LogInformation($"imageUri: {imageUri}");
                log.LogInformation($"storageAccountName: {storageAccountName}");
                log.LogInformation($"osType: {osType}");
                log.LogInformation($"computerName: {computerName}");
                log.LogInformation($"username: {username}");
                log.LogInformation($"password: {password}");


                IAzure _azure = Azure.Configure()
                      .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                      .Authenticate(credentials)
                      .WithSubscription(subscriptionId);



                log.LogInformation(_azure.GetCurrentSubscription().SubscriptionId);

                log.LogInformation("Getting labs dependencies");

                string resourceGroupName = (isVersion2_2 ? $"cs-{clientCode}-{environment}-rgrp" : labsProvision.ResourceGroupName).ToUpper();
                string networkSecurityGroupName = (isVersion2_2 ? $"cs-{clientCode}-{environment}-nsg" : $"CS-{environment}-{clientCode}-nsg").ToUpper();
                string virtualNetworkName = (isVersion2_2 ? $"cs-{clientCode}-{environment}-vnet" : $"CS-{environment}-{clientCode}-vnet").ToUpper();

                log.LogInformation("Setting up tags");

                object rgTags = new
                {
                    _business_name = "cs",
                    _azure_region = location,
                    _contact_person = contactPerson,
                    _client_code = clientCode,
                    _environment = environment,
                    _lab_type = "virtualmachine",
                    _created = DateTime.Now.ToShortDateString(),
                };


                log.LogInformation("Getting labs resource dependencies");
                string vnetId = _azure.Networks.GetByResourceGroup(resourceGroupName, virtualNetworkName).Id.ToString();
                string nsgId = _azure.NetworkSecurityGroups.GetByResourceGroup(resourceGroupName, networkSecurityGroupName).Id.ToString();
                INetwork virtualNetwork = _azure.Networks.GetById(vnetId);

                //string publicIpAddressType = virtualNetwork.Inner.Subnets[0].NatGateway == null ? "Dynamic" : "Static";
                //string publicIpAddressSku = virtualNetwork.Inner.Subnets[0].NatGateway == null ? "Basic" : "Standard";
                //string publicIpAddressType = "Static";
                //string publicIpAddressSku = "Standard";

                NewTenantModel newTenantModel = new NewTenantModel()
                {
                    applicationId = applicationId,
                    applicationKey = applicationKey,
                    subscriptionId = subscriptionId,
                    tenantId = tenantId,
                    clientCode = clientCode,
                    environment = environment,
                    contactEmail = contactPerson,
                    location = location
                };


                log.LogInformation("Getting labs resource group name");

                string labsResourceGroupName = isVersion2_2 ? await SetResourceGroupAsync(newTenantModel, log, _azure, credentials, subscriptionId, location, environment, clientCode, contactPerson) : _azure.ResourceGroups.GetByName(resourceGroupName).Name;
                log.LogInformation($"RG: {labsResourceGroupName}");

                string cc = labsResourceGroupName.Split('-')[1];

                if (!clientCode.Equals(cc))
                {
                    log.LogInformation("Not Equal");
                    try {
                        int incrementor = int.Parse(cc.Split(clientCode)[1]);
                        if (incrementor != 1) {
                            resourceGroupName = (isVersion2_2 ? $"cs-{clientCode}{incrementor}-{environment}-rgrp" : labsProvision.ResourceGroupName).ToUpper();
                            networkSecurityGroupName = (isVersion2_2 ? $"cs-{clientCode}{incrementor}-{environment}-nsg" : $"CS-{environment}-{clientCode}-nsg").ToUpper();
                            virtualNetworkName = (isVersion2_2 ? $"cs-{clientCode}{incrementor}-{environment}-vnet" : $"CS-{environment}-{clientCode}-vnet").ToUpper();

                            log.LogInformation("Getting labs resource dependencies");
                            vnetId = _azure.Networks.GetByResourceGroup(resourceGroupName, virtualNetworkName).Id.ToString();
                            nsgId = _azure.NetworkSecurityGroups.GetByResourceGroup(resourceGroupName, networkSecurityGroupName).Id.ToString();
                            virtualNetwork = _azure.Networks.GetById(vnetId);
                        }
                    }
                    catch(Exception ex){
                        log.LogInformation(ex.Message);
                    }
                }


                log.LogInformation($"RG: {resourceGroupName}");
                log.LogInformation($"NSG: {networkSecurityGroupName}");
                log.LogInformation($"VNET: {virtualNetworkName}");

                Stream stream;// = new MemoryStream(Properties.Resources.azuredeploywindows);
                JObject templateParameterObjectVirtualMachine = new JObject();
                StreamReader template;// = new StreamReader(stream);
                JsonTextReader reader;// = new JsonTextReader(template);

                switch (osType.ToUpper())
                {
                    case "WINDOWS":
                        {
                            if (isManaged)
                            {
                                log.LogInformation("INF | OS DISK TYPE: MANAGED");
                                stream = new MemoryStream(Properties.Resources.azuredeploywindowsmanaged);
                                template = new StreamReader(stream);
                                reader = new JsonTextReader(template);
                                templateParameterObjectVirtualMachine = (JObject)JToken.ReadFrom(reader);
                                templateParameterObjectVirtualMachine.SelectToken("parameters.imageReferenceId")["defaultValue"] = imageReferenceId;
                            }
                            else
                            {
                                log.LogInformation("INF | OS DISK TYPE: UNMANAGED");
                                stream = new MemoryStream(Properties.Resources.azuredeploywindowsfirewall);
                                template = new StreamReader(stream);
                                reader = new JsonTextReader(template);
                                templateParameterObjectVirtualMachine = (JObject)JToken.ReadFrom(reader);
                                templateParameterObjectVirtualMachine.SelectToken("parameters.imageUri")["defaultValue"] = imageUri;
                            }
                            templateParameterObjectVirtualMachine.SelectToken("parameters.fileUris")["defaultValue"] = createEnvironmentVariablesPsUrl;
                            templateParameterObjectVirtualMachine.SelectToken("parameters.arguments")["defaultValue"] = $"-ResourceGroupName {labsResourceGroupName} -VirtualMachineName {virtualMachineName} -ComputerName {computerName} -TenantId {tenantId} -GroupCode {apiprefix} -Fqdn {fqdn}";


                            break;
                        }
                    case "UBUNTU":
                    case "LINUX":
                        {
                            stream = new MemoryStream(Properties.Resources.azuredeploylinux);
                            template = new StreamReader(stream);
                            reader = new JsonTextReader(template);
                            templateParameterObjectVirtualMachine = (JObject)JToken.ReadFrom(reader);
                            break;
                        }
                    default:
                        {
                            return new BadRequestObjectResult(
                                JsonConvert.SerializeObject(new
                                {
                                    message = "Incorect OsType. Must be WINDOWS, UBUNTU or LINUX",
                                })
                            );
                        }
                }


                templateParameterObjectVirtualMachine.SelectToken("parameters.location")["defaultValue"] = location;
                templateParameterObjectVirtualMachine.SelectToken("parameters.networkSecurityGroupId")["defaultValue"] = nsgId;
                templateParameterObjectVirtualMachine.SelectToken("parameters.subnetName")["defaultValue"] = "default";
                templateParameterObjectVirtualMachine.SelectToken("parameters.virtualNetworkId")["defaultValue"] = vnetId;
                templateParameterObjectVirtualMachine.SelectToken("parameters.virtualMachineName")["defaultValue"] = virtualMachineName;
                templateParameterObjectVirtualMachine.SelectToken("parameters.computerName")["defaultValue"] = computerName;
                templateParameterObjectVirtualMachine.SelectToken("parameters.storageAccountName")["defaultValue"] = storageAccountName;
                //templateParameterObjectVirtualMachine.SelectToken("parameters.publicIpAddressType")["defaultValue"] = publicIpAddressType;
                //templateParameterObjectVirtualMachine.SelectToken("parameters.publicIpAddressSku")["defaultValue"] = publicIpAddressSku;
                templateParameterObjectVirtualMachine.SelectToken("parameters.publicIpAddressType")["defaultValue"] = "Static";
                templateParameterObjectVirtualMachine.SelectToken("parameters.publicIpAddressSku")["defaultValue"] = "Standard";
                templateParameterObjectVirtualMachine.SelectToken("parameters.virtualMachineSize")["defaultValue"] = size;
                templateParameterObjectVirtualMachine.SelectToken("parameters.adminUsername")["defaultValue"] = username;
                templateParameterObjectVirtualMachine.SelectToken("parameters.adminPassword")["defaultValue"] = password;
                templateParameterObjectVirtualMachine.SelectToken("parameters.newTemplateName")["defaultValue"] = virtualMachineName;
                templateParameterObjectVirtualMachine.SelectToken("parameters.tags")["defaultValue"] = JToken.FromObject(rgTags);
                templateParameterObjectVirtualMachine.SelectToken("parameters.diskSizeGB")["defaultValue"] = tempStorageSizeInGb;

                string deploymentName = $"virtual-machine-{uniqueId}".ToLower();
                log.LogInformation($"Deploying virtual-machine-{uniqueId}".ToLower());

                IDeployment vmDeployment = _azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(labsResourceGroupName)
                    .WithTemplate(templateParameterObjectVirtualMachine)
                    .WithParameters("{}")
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                    .Create();


                if (vmDeployment.ProvisioningState.Value == ProvisioningState.Failed.Value)
                {
                    log.LogInformation("End of Provisioning");
                    log.LogError("Deployment Failed");

                    return new BadRequestObjectResult(JsonConvert.SerializeObject(new
                    {
                        message = "Deployment Failed",
                        result = false
                    }));
                }

                log.LogInformation("End of Provisioning");

                return new OkObjectResult(JsonConvert.SerializeObject(new
                {
                    message = "Deployment is done",
                    result = true
                }));


            }
            catch (Exception e)
            {
                log.LogError(e.Message);

                return new BadRequestObjectResult(JsonConvert.SerializeObject(new
                {
                    message = e.Message,
                    result = false
                }));
            }
        }

        public static async Task<string> SetResourceGroupAsync(NewTenantModel newTenantModel, ILogger log, IAzure _azure, AzureCredentials credentials, string subscriptionId, string location, string environment, string clientCode, string contactPerson, int maxRgLimit = 50, int maxResources = 15)
        {
            IDictionary<string, string> rgTags = new Dictionary<string, string>();

            rgTags.Add("_business_name", "cs");
            rgTags.Add("_azure_region", location);
            rgTags.Add("_contact_person", contactPerson);
            rgTags.Add("_client_code", clientCode.ToUpper());
            rgTags.Add("_environment", environment.ToUpper());

            string resourceGroupName = string.Empty;

            RestClient _restClient = RestClient
                  .Configure()
                  .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                  .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                  .WithCredentials(credentials)
                  .Build();

            for (int i = 1; i <= maxRgLimit; i++)
            {
                resourceGroupName = $"cs-{clientCode}{i}-{environment}-rgrp".ToUpper();
                try
                {
                    IResourceGroup resourceGroup = _azure.ResourceGroups.GetByName(resourceGroupName);
                }
                catch
                {
                    _azure.ResourceGroups.Define(resourceGroupName)
                               .WithRegion(location)
                               .WithTags(rgTags)
                               .Create();


                    if (i != 1)
                    {
                        await FirewallProvision(log, newTenantModel, i);
                    }
                }

                ResourceManagementClient resourceManagementClient = new ResourceManagementClient(_restClient)
                {
                    SubscriptionId = subscriptionId
                };

                int resourceCount = (await resourceManagementClient.Resources.ListByResourceGroupAsync(resourceGroupName)).ToList().Count;

                if (resourceCount <= maxResources)
                {
                    break;
                }

            }

            return resourceGroupName;
        }

        public static async Task FirewallProvision(ILogger log, NewTenantModel newTenantModel, int incrementor) {

            if (string.IsNullOrEmpty(newTenantModel.applicationId) ||
                string.IsNullOrEmpty(newTenantModel.applicationKey) ||
                string.IsNullOrEmpty(newTenantModel.subscriptionId) ||
                string.IsNullOrEmpty(newTenantModel.tenantId) ||
                string.IsNullOrEmpty(newTenantModel.clientCode) ||
                string.IsNullOrEmpty(newTenantModel.environment) ||
                string.IsNullOrEmpty(newTenantModel.contactEmail) ||
                string.IsNullOrEmpty(newTenantModel.location))
            {
                log.LogInformation("Incorect Request Body.");
                log.LogInformation(JsonConvert.SerializeObject(newTenantModel));
            }

            string applicationId = newTenantModel.applicationId;
            string applicationKey = newTenantModel.applicationKey;
            string subscriptionId = newTenantModel.subscriptionId;
            string tenantId = newTenantModel.tenantId;
            string clientCode = newTenantModel.clientCode;
            string environment = newTenantModel.environment;
            string contactEmail = newTenantModel.contactEmail;
            string location = newTenantModel.location;

            log.LogInformation("Generating New Tenant");
            string resourceGroupName = $"CS-{clientCode}{incrementor}-{environment}-RGRP".ToUpper();
            string nsgName = $"CS-{clientCode}{incrementor}-{environment}-NSG".ToUpper();
            string vNetName = $"CS-{clientCode}{incrementor}-{environment}-vnet".ToUpper();
            string firewallRouteName = $"CS-{clientCode}{incrementor}-{environment}-fwr".ToUpper();
            string firewallName = $"CS-{clientCode}{incrementor}-{environment}-fw".ToUpper();
            string azure_firewalls_puplic_ip_name = $"CS-{clientCode}{incrementor}-{environment}-fw-ip".ToLower();

            log.LogInformation($"resourceGroupName: {resourceGroupName}");
            log.LogInformation($"nsgName: {nsgName}");
            log.LogInformation($"vNetName: {vNetName}");

            log.LogInformation("Setting up tags");

            IDictionary<string, string> iTags = new Dictionary<string, string>();
            iTags.Add("_business_name", "cs");
            iTags.Add("_azure_region", location);
            iTags.Add("_contact_person", contactEmail);
            iTags.Add("_client_code", clientCode);
            iTags.Add("_environment", environment);
            iTags.Add("_lab_type", "virtualmachine");
            iTags.Add("_created", DateTime.Now.ToShortDateString());

            object rgTags = new
            {
                _business_name = "cs",
                _azure_region = location,
                _contact_person = contactEmail,
                _client_code = clientCode,
                _environment = environment,
                _lab_type = "virtualmachine",
                _created = DateTime.Now.ToShortDateString(),
            };

            log.LogInformation("Checking Authentication");

            try
            {
                ServicePrincipalLoginInformation principalLogIn = new ServicePrincipalLoginInformation();
                principalLogIn.ClientId = applicationId;
                principalLogIn.ClientSecret = applicationKey;

                AzureEnvironment azureEnvironment = AzureEnvironment.AzureGlobalCloud;
                AzureCredentials credentials = new AzureCredentials(principalLogIn, tenantId, azureEnvironment);

                IAzure _azure = Azure.Configure()
                      .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                      .Authenticate(credentials)
                      .WithSubscription(subscriptionId);

                log.LogInformation($"SubscriptionId: {_azure.GetCurrentSubscription().SubscriptionId}");

                string uniqueId = Guid.NewGuid().ToString().Replace("-", "");

                JObject templateParameterObjectFirewall = ResourceHelper.GetJObject(Properties.Resources.firewall);

                IResourceGroup resourceGroup;
                INetworkSecurityGroup firewall;

                try
                {
                    resourceGroup = _azure.ResourceGroups.GetByName(resourceGroupName);
                    if (resourceGroup != null)
                    {
                        log.LogInformation($"Resource Group {resourceGroup.Name} already exist.");
                    }
                }
                catch
                {
                    resourceGroup = _azure.ResourceGroups.Define(resourceGroupName)
                        .WithRegion(location)
                        .WithTags(iTags)
                        .Create();
                }

                firewall = _azure.NetworkSecurityGroups.GetByResourceGroup(resourceGroupName, nsgName);
                if (firewall == null)
                {
                    templateParameterObjectFirewall.SelectToken("parameters.azure_firewalls_puplic_ip_name")["defaultValue"] = azure_firewalls_puplic_ip_name;
                    templateParameterObjectFirewall.SelectToken("parameters.virtualnetwork_name")["defaultValue"] = vNetName;
                    templateParameterObjectFirewall.SelectToken("parameters.routetables_firewall_route_name")["defaultValue"] = firewallRouteName;
                    templateParameterObjectFirewall.SelectToken("parameters.azure_firewalls_name")["defaultValue"] = firewallName;
                    templateParameterObjectFirewall.SelectToken("parameters.location")["defaultValue"] = location;
                    templateParameterObjectFirewall.SelectToken("parameters.nsgName")["defaultValue"] = nsgName;
                    templateParameterObjectFirewall.SelectToken("parameters.tags")["defaultValue"] = JToken.FromObject(rgTags);

                    log.LogInformation("Deploying Network Security Group");
                    IDeployment networkSecurityGroupDeployment = await _azure.Deployments
                        .Define($"firewall-{uniqueId}")
                        .WithExistingResourceGroup(resourceGroup)
                        .WithTemplate(templateParameterObjectFirewall)
                        .WithParameters("{}")
                        .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                        .CreateAsync();
                    log.LogInformation("Network Security Group is done");
                }


                log.LogInformation("End of New Tenant Creation");

                log.LogInformation($"resourcegroup = {resourceGroupName}");
                log.LogInformation($"nsg = {nsgName}");
                log.LogInformation($"vnet = {vNetName}");
            }
            catch (Exception e)
            {
                log.LogInformation("End of New Tenant Creation");
                log.LogError("Deployment Failed");
                log.LogError(e.Message);
            }

        }

    }
}
