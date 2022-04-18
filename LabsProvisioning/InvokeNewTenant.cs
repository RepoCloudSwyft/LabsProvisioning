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

namespace LabsProvisioning
{
    public static class InvokeNewTenant
    {
        [FunctionName("InvokeNewTenant")]
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
                string.IsNullOrEmpty(newTenantModel.location) ||
                string.IsNullOrEmpty(newTenantModel.appServicePlanName) ||
                string.IsNullOrEmpty(newTenantModel.appServicePlanResourceGroupName) ||
                //string.IsNullOrEmpty(newTenantModel.storageAccountName) ||
                string.IsNullOrEmpty(newTenantModel.clientName) ||
                string.IsNullOrEmpty(newTenantModel.labsDataBase) ||
                string.IsNullOrEmpty(newTenantModel.labsDataServer) ||
                string.IsNullOrEmpty(newTenantModel.labsPassword) ||
                string.IsNullOrEmpty(newTenantModel.labsUserId))
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

            string applicationId                    = newTenantModel.applicationId;
            string applicationKey                   = newTenantModel.applicationKey;
            string subscriptionId                   = newTenantModel.subscriptionId;
            string tenantId                         = newTenantModel.tenantId;
            string clientCode                       = newTenantModel.clientCode;
            string environment                      = newTenantModel.environment;
            string contactEmail                     = newTenantModel.contactEmail;
            string location                         = newTenantModel.location;
            string appServicePlanName               = newTenantModel.appServicePlanName;
            string appServicePlanResourceGroupName  = newTenantModel.appServicePlanResourceGroupName;
            string CC                               = ResourceHelper.GetEnvironmentVariable("CC");
            string ClientName                       = newTenantModel.clientName;
            string FROM                             = ResourceHelper.GetEnvironmentVariable("FROM");
            string FROM_DAILYREPORT_EMAIL           = ResourceHelper.GetEnvironmentVariable("FROM_DAILYREPORT_EMAIL");
            string LabsDataBase                     = newTenantModel.labsDataBase;
            string LabsDataServer                   = newTenantModel.labsDataServer;
            string LabsPassword                     = newTenantModel.labsPassword;
            string LabsUserId                       = newTenantModel.labsUserId;
            string SendGridKey                      = ResourceHelper.GetEnvironmentVariable("SendGridKey");
            string SendGridName                     = ResourceHelper.GetEnvironmentVariable("SendGridName");
            string TO                               = ResourceHelper.GetEnvironmentVariable("TO");
            string WEBSITE_TIME_ZONE                = ResourceHelper.GetEnvironmentVariable("WEBSITE_TIME_ZONE");

            log.LogInformation("Generating New Tenant");
            string resourceGroupName    = $"cs-{clientCode}-{environment}-rgrp".ToUpper();
            string nsgName              = $"cs-{clientCode}-{environment}-nsg".ToUpper();
            string vNetName             = $"cs-{clientCode}-{environment}-vnet".ToUpper();
            string faName               = $"cs-{clientCode}-{environment}-fsdr".ToUpper();
            string storageAccountName   = $"cs{clientCode}{environment}vhdsa".ToLower();

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

            try {
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

                // New-AzApplicationInsights -ResourceGroupName $resourceGroupName -Name $applicationInsightName -Location $location -ErrorVariable ErrApplicationInsight -ErrorAction SilentlyContinue
                PowerShell ps = PowerShell.Create();

                //ps.AddScript("Install-Module -Name Az -AllowClobber -Force");
                //ps.Invoke();
                //ps.AddScript("Import-Module -Name Az -Force");
                //ps.Invoke();

                //ps.AddCommand("New-AzApplicationInsights");
                //ps.AddParameter("ResourceGroupName", resourceGroupName);
                //ps.AddParameter("Name", faName);
                //ps.AddParameter("Location", location);
                //ps.Invoke();
                ps.AddScript("Get-Module -ListAvailable");
                Collection<PSObject> PSOutput = ps.Invoke();

                // loop through each output object item
                foreach (PSObject outputItem in PSOutput)
                {
                    // if null object was dumped to the pipeline during the script then a null
                    // object may be present here. check for null to prevent potential NRE.
                    if (outputItem != null)
                    {
                        //TODO: do something with the output item 
                        // outputItem.BaseOBject
                        log.LogInformation(outputItem.BaseObject.ToString());
                    }
                }

                return new OkObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        message = $"ok"
                    })
                );
                IResourceGroup resourceGroups = _azure.ResourceGroups.List().Where(rg => rg.Name.Equals(resourceGroupName)).FirstOrDefault();

                if (resourceGroups != null)
                {
                    log.LogInformation($"Resource group exist: {resourceGroupName}");
                    log.LogInformation("End of New Tenant Creation");
                    return new OkObjectResult(
                        JsonConvert.SerializeObject(new
                        {
                            message = $"Resource group exist: {resourceGroupName}"
                        })
                    );
                }


                string uniqueId = Guid.NewGuid().ToString().Replace("-","");

                JObject templateParameterObjectNSG = ResourceHelper.GetJObject(Properties.Resources.nsg_web_app_network);

                JObject templateParameterObjectVNet = ResourceHelper.GetJObject(Properties.Resources.azuredeploy_network);

                JObject templateParameterObjectFSDR = ResourceHelper.GetJObject(Properties.Resources.forcestop_dailyreport);

                JObject templateParameterObjectAppInsight = ResourceHelper.GetJObject(Properties.Resources.app_insight);

                templateParameterObjectAppInsight.SelectToken("parameters.location")["defaultValue"] = location;
                templateParameterObjectAppInsight.SelectToken("parameters.name")["defaultValue"] = faName;
                templateParameterObjectAppInsight.SelectToken("parameters.subscriptionId")["defaultValue"] = subscriptionId;


                templateParameterObjectNSG.SelectToken("parameters.location")["defaultValue"] = location;
                templateParameterObjectNSG.SelectToken("parameters.nsgName")["defaultValue"] = nsgName;
                templateParameterObjectNSG.SelectToken("parameters.tags")["defaultValue"] = JToken.FromObject(rgTags);

                templateParameterObjectVNet.SelectToken("parameters.location")["defaultValue"] = location;
                templateParameterObjectVNet.SelectToken("parameters.tags")["defaultValue"] = JToken.FromObject(rgTags);
                templateParameterObjectVNet.SelectToken("parameters.virtualNetworkName")["defaultValue"] = vNetName;
                templateParameterObjectVNet.SelectToken("parameters.virtualMachineSubnetName")["defaultValue"] = "virtual-machines-labs-subnet";
                templateParameterObjectVNet.SelectToken("parameters.nsgName")["defaultValue"] = nsgName;

                templateParameterObjectFSDR.SelectToken("parameters.subscriptionId")["defaultValue"] = subscriptionId;
                templateParameterObjectFSDR.SelectToken("parameters.functionAppName")["defaultValue"] = faName;
                templateParameterObjectFSDR.SelectToken("parameters.location")["defaultValue"] = location;
                templateParameterObjectFSDR.SelectToken("parameters.appServicePlanName")["defaultValue"] = appServicePlanName;
                templateParameterObjectFSDR.SelectToken("parameters.appServicePlanResourceGroupName")["defaultValue"] = appServicePlanResourceGroupName;
                templateParameterObjectFSDR.SelectToken("parameters.resourceGroupName")["defaultValue"] = resourceGroupName;
                templateParameterObjectFSDR.SelectToken("parameters.storageAccountName")["defaultValue"] = storageAccountName;
                templateParameterObjectFSDR.SelectToken("parameters.CC")["defaultValue"] = $"'{CC}'";
                templateParameterObjectFSDR.SelectToken("parameters.ClientCode")["defaultValue"] = clientCode;
                templateParameterObjectFSDR.SelectToken("parameters.ClientId")["defaultValue"] = applicationId;
                templateParameterObjectFSDR.SelectToken("parameters.ClientKey")["defaultValue"] = applicationKey;
                templateParameterObjectFSDR.SelectToken("parameters.ClientName")["defaultValue"] = ClientName;
                templateParameterObjectFSDR.SelectToken("parameters.FROM")["defaultValue"] = FROM;
                templateParameterObjectFSDR.SelectToken("parameters.FROM_DAILYREPORT_EMAIL")["defaultValue"] = FROM_DAILYREPORT_EMAIL;
                templateParameterObjectFSDR.SelectToken("parameters.LabsDataBase")["defaultValue"] = LabsDataBase;
                templateParameterObjectFSDR.SelectToken("parameters.LabsDataServer")["defaultValue"] = LabsDataServer;
                templateParameterObjectFSDR.SelectToken("parameters.LabsPassword")["defaultValue"] = LabsPassword;
                templateParameterObjectFSDR.SelectToken("parameters.LabsUserId")["defaultValue"] = LabsUserId;
                templateParameterObjectFSDR.SelectToken("parameters.SendGridKey")["defaultValue"] = SendGridKey;
                templateParameterObjectFSDR.SelectToken("parameters.SendGridName")["defaultValue"] = SendGridName;
                templateParameterObjectFSDR.SelectToken("parameters.TO")["defaultValue"] = $"'{TO}'";
                templateParameterObjectFSDR.SelectToken("parameters.WEBSITE_TIME_ZONE")["defaultValue"] = WEBSITE_TIME_ZONE;
                templateParameterObjectFSDR.SelectToken("parameters.TenantId")["defaultValue"] = tenantId;

                IResourceGroup resourceGroup = _azure.ResourceGroups
                    .Define(resourceGroupName)
                    .WithRegion(location)
                    .WithTags(iTags)
                    .Create();

                log.LogInformation("Deploying Network Security Group");
                IDeployment networkSecurityGroupDeployment = await _azure.Deployments
                    .Define($"nsg-{uniqueId}")
                    .WithExistingResourceGroup(resourceGroup)
                    .WithTemplate(templateParameterObjectNSG)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync();
                log.LogInformation("Network Security Group is done");

                log.LogInformation("Deploying Application Insight");
                IDeployment appInsightDeployment = await _azure.Deployments
                    .Define($"app-insihgt-{uniqueId}")
                    .WithExistingResourceGroup(resourceGroup)
                    .WithTemplate(templateParameterObjectAppInsight)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync();
                log.LogInformation("Application Insight is done");

                log.LogInformation("Deploying Virtual Network");
                IDeployment virtualNetworkDeployment = await _azure.Deployments
                    .Define($"vnet-{uniqueId}")
                    .WithExistingResourceGroup(resourceGroup)
                    .WithTemplate(templateParameterObjectVNet)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync();
                log.LogInformation("Virtual Network is done");


                log.LogInformation("Deploying Function App");
                IDeployment functionAppDeployment = await _azure.Deployments
                    .Define($"fa-{uniqueId}")
                    .WithExistingResourceGroup(resourceGroup)
                    .WithTemplate(templateParameterObjectFSDR)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync();
                log.LogInformation("Function App is done");


                log.LogInformation("End of New Tenant Creation");

                return new OkObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        resourcegroup = resourceGroupName,
                        nsg = nsgName,
                        vnet = vNetName,
                        fa = faName
                    })
                );
            }
            catch (Exception e) {
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
