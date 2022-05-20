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
    public static class LabsProvision2_2_1
    {
        [FunctionName("LabsProvision2_2_1")]
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
                string.IsNullOrEmpty(labsProvision.ImageUri) ||
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

            string createEnvironmentVariablesPsUrl = ResourceHelper.GetEnvironmentVariable("CreateEnvironmentVariablesPsUrl");

            bool isVersion2_2 = string.IsNullOrEmpty(labsProvision.ResourceGroupName);

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

                string publicIpAddressType = virtualNetwork.Inner.Subnets[0].NatGateway == null ? "Dynamic" : "Static";
                string publicIpAddressSku = virtualNetwork.Inner.Subnets[0].NatGateway == null ? "Basic" : "Standard";

                log.LogInformation("Getting labs resource group name");

                string labsResourceGroupName = isVersion2_2 ? await SetResourceGroupAsync(_azure, credentials, subscriptionId, location, environment, clientCode, contactPerson) : _azure.ResourceGroups.GetByName(resourceGroupName).Name;

                Stream stream;// = new MemoryStream(Properties.Resources.azuredeploywindows);
                JObject templateParameterObjectVirtualMachine = new JObject();
                StreamReader template;// = new StreamReader(stream);
                JsonTextReader reader;// = new JsonTextReader(template);

                switch (osType.ToUpper())
                {
                    case "WINDOWS":
                        {
                            stream = new MemoryStream(Properties.Resources.azuredeploywindows);
                            template = new StreamReader(stream);
                            reader = new JsonTextReader(template);
                            templateParameterObjectVirtualMachine = (JObject)JToken.ReadFrom(reader);
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
                templateParameterObjectVirtualMachine.SelectToken("parameters.publicIpAddressType")["defaultValue"] = publicIpAddressType;
                templateParameterObjectVirtualMachine.SelectToken("parameters.publicIpAddressSku")["defaultValue"] = publicIpAddressSku;
                templateParameterObjectVirtualMachine.SelectToken("parameters.virtualMachineSize")["defaultValue"] = size;
                templateParameterObjectVirtualMachine.SelectToken("parameters.adminUsername")["defaultValue"] = username;
                templateParameterObjectVirtualMachine.SelectToken("parameters.adminPassword")["defaultValue"] = password;
                templateParameterObjectVirtualMachine.SelectToken("parameters.newTemplateName")["defaultValue"] = virtualMachineName;
                templateParameterObjectVirtualMachine.SelectToken("parameters.imageUri")["defaultValue"] = imageUri;
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

                //log.LogInformation($"Deallocate when done: {deallocateWhenFinish}");
                //if (deallocateWhenFinish)
                //{
                //    try
                //    {
                //        log.LogInformation($"Deallocating {virtualMachineName}");
                //        IVirtualMachine virtualMachine = _azure.VirtualMachines.GetByResourceGroup(labsResourceGroupName, virtualMachineName);
                //        virtualMachine.Deallocate();
                //        log.LogInformation($"Deallocated");
                //        log.LogInformation("End of Provisioning");

                //        return new OkObjectResult(JsonConvert.SerializeObject(new
                //        {
                //            message = "Deployment is done",
                //            result = true
                //        }));
                //    }
                //    catch (Exception e)
                //    {

                //        log.LogError(e.Message);
                //        log.LogError("End of Provisioning");

                //        return new BadRequestObjectResult(JsonConvert.SerializeObject(new
                //        {
                //            message = e.Message,
                //            result = false
                //        }));
                //    }
                //}

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

        public static async Task<string> SetResourceGroupAsync(IAzure _azure, AzureCredentials credentials, string subscriptionId, string location, string environment, string clientCode, string contactPerson, int maxRgLimit = 50, int maxResources = 750)
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

    }
}
