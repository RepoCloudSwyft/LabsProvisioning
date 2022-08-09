using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;


namespace LabsProvisioning
{
    public static class GetStorageConnections
    {
        [FunctionName("GetStorageConnections")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            GetStorageConnectionsModel getStorageConnections = JsonConvert.DeserializeObject<GetStorageConnectionsModel>(requestBody);

            if (string.IsNullOrEmpty(getStorageConnections.applicationId) ||
                string.IsNullOrEmpty(getStorageConnections.applicationKey) ||
                string.IsNullOrEmpty(getStorageConnections.subscriptionId) ||
                string.IsNullOrEmpty(getStorageConnections.tenantId))
            {
                log.LogInformation("Incorect Request Body.");
                log.LogInformation(JsonConvert.SerializeObject(getStorageConnections));
                return new BadRequestObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        message = "Incorect Request Body.",
                        requestBody = JsonConvert.SerializeObject(getStorageConnections)
                    })
                );
            }

            try
            {
                ServicePrincipalLoginInformation principalLogIn = new ServicePrincipalLoginInformation();
                principalLogIn.ClientId = getStorageConnections.applicationId;
                principalLogIn.ClientSecret = getStorageConnections.applicationKey;

                AzureEnvironment azureEnvironment = AzureEnvironment.AzureGlobalCloud;
                AzureCredentials credentials = new AzureCredentials(principalLogIn, getStorageConnections.tenantId, azureEnvironment);

                IAzure _azure = Azure.Configure()
                      .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                      .Authenticate(credentials)
                      .WithSubscription(getStorageConnections.subscriptionId);

                List<IStorageAccount> storageAccounts = _azure.StorageAccounts.List()
                    .Where(sa => sa.Name.Contains("disk"))
                    .ToList();
                List<object> connections = new List<object>();
                foreach (IStorageAccount storageAccount in storageAccounts)
                {
                    StorageCredentials storageCredentials = new StorageCredentials(storageAccount.Name, storageAccount.GetKeys()[0].Value);
                    CloudStorageAccount cloudStorageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
                    log.LogInformation(storageAccount.Name);
                    string connectionstring = $"DefaultEndpointsProtocol=https;AccountName={storageAccount.Name};AccountKey={storageAccount.GetKeys()[0].Value};EndpointSuffix=core.windows.net";
                    connections.Add(
                        new
                        {
                            storageAccountName = storageAccount.Name,
                            connectionstring = connectionstring,
                            storageKey = storageAccount.GetKeys()[0].Value
                        }
                    );
                }

                return new OkObjectResult(JsonConvert.SerializeObject(connections));

            }
            catch (Exception e)
            {

                return new BadRequestObjectResult(
                    JsonConvert.SerializeObject(new
                    {
                        exeption = e.Message,
                    })
                );

            }

        }
    }
}
