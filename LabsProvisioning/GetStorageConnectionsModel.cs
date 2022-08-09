using System;
using System.Collections.Generic;
using System.Text;

namespace LabsProvisioning
{
    public class GetStorageConnectionsModel
    {
        public string subscriptionId { get; set; }
        public string tenantId { get; set; }
        public string applicationId { get; set; }
        public string applicationKey { get; set; }
    }
}
