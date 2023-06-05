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
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Management.Automation;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using System.Collections.ObjectModel;
using Microsoft.Azure.Management.Network.Fluent;

namespace LabsProvisioning
{
    public static class InvokeNewTenantWithFW
    {
        [FunctionName("InvokeNewTenantWithFW")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Start Create New Tenant");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            NewTenantModel newTenantModel = JsonConvert.DeserializeObject<NewTenantModel>(requestBody);

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
                return new BadRequestObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        message = "Incorect Request Body.",
                        requestBody = JsonConvert.SerializeObject(newTenantModel)
                    })
                );
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
            string resourceGroupName = $"CS-{clientCode}-{environment}-RGRP".ToUpper();
            string nsgName = $"CS-{clientCode}-{environment}-NSG".ToUpper();
            string vNetName = $"CS-{clientCode}-{environment}-vnet".ToUpper();
            string firewallRouteName = $"CS-{clientCode}-{environment}-fwr".ToUpper();
            string firewallName = $"CS-{clientCode}-{environment}-fw".ToUpper();

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
                        return new BadRequestObjectResult(
                            JsonConvert.SerializeObject(new
                            {
                                message = $"Resource Group {resourceGroup.Name} already exist.",
                            })
                        );
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
                        .WithMode(DeploymentMode.Incremental)
                        .CreateAsync();
                    log.LogInformation("Network Security Group is done");
                }


                log.LogInformation("End of New Tenant Creation");

                return new OkObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        resourcegroup = resourceGroupName,
                        nsg = nsgName,
                        vnet = vNetName
                    })
                );
            }
            catch (Exception e)
            {
                log.LogInformation("End of New Tenant Creation");
                log.LogError("Deployment Failed");
                return new BadRequestObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        message = e.Message
                    })
                );
            }

        }
    }
}
