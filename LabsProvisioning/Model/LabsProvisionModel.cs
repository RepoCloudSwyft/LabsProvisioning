using Microsoft.Azure.Management.Compute.Fluent.Models;

namespace LabsProvisioning.Model
{
    public class LabsProvisionModel
    {
        public string ResourceGroupName { get; set; }

        public string Location { get; set; }

        public string Environment { get; set; }

        public string ClientCode { get; set; }

        public string VirtualMachineName { get; set; }

        public string OsType { get; set; }

        public string Size { get; set; } = VirtualMachineSizeTypes.StandardB2ms.Value;

        public bool IsCustomTemplate { get; set; } = false;

        public int TempStorageSizeInGb { get; set; } = 127;

        public string ImageUri { get; set; }

        public string ContactPerson { get; set; }

        public string StorageAccountName { get; set; }

        public string ComputerName { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string SubscriptionId { get; set; }

        public string ApplicationId { get; set; }

        public string ApplicationKey { get; set; }

        public string TenantId { get; set; }

        public bool DeallocateWhenFinish { get; set; } = false;

        public string Fqdn { get; set; }

        public string apiprefix { get; set; }

    }
}
