using System;
using System.Collections.Generic;
using System.Text;

namespace LabsProvisioning.Model
{
    public class TemplateParameterVmModel
    {
        public string networkSecurityGroupId { get; set; }

        public string subnetName { get; set; } = "virtual-machines-labs-subnet";

        public string virtualNetworkId { get; set; }

        public string virtualMachineName { get; set; }

        public string computerName { get; set; }

        public string adminUsername { get; set; }

        public string adminPassword { get; set; }
        
        public string storageAccountName { get; set; }
        
        public string imageUri { get; set; }
        
        public object tags { get; set; }
        
        public string virtualMachineSize { get; set; }
        
        public int diskSizeGB { get; set; }
        
        public string newTemplateName { get; set; }
        
        public string publicIpAddressType { get; set; }
        
        public string publicIpAddressSku { get; set; }
    }
}
