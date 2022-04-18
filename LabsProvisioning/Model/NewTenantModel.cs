using System;
using System.Collections.Generic;
using System.Text;

namespace LabsProvisioning.Model
{
    public class NewTenantModel
    {
        public string subscriptionId { get; set; }
        
        public string tenantId { get; set; }
        
        public string applicationKey { get; set; }

        public string applicationId { get; set; }

        public string clientCode { get; set; }

        public string environment { get; set; }

        public string contactEmail { get; set; }

        public string location { get; set; }

        public string appServicePlanName { get; set; }

        public string appServicePlanResourceGroupName { get; set; }

        //public string storageAccountName { get; set; }

        public string clientName { get; set; }

        public string labsDataBase { get; set; }

        public string labsDataServer { get; set; }

        public string labsPassword { get; set; }

        public string labsUserId { get; set; }

    }
}
