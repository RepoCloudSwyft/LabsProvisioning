using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LabsProvisioning
{
    public static class ResourceHelper
    {
        public static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        public static JObject GetJObject(byte[] resourceTemplate) {
            Stream stream = new MemoryStream(resourceTemplate);
            StreamReader template = new StreamReader(stream);
            JsonTextReader reader = new JsonTextReader(template);
            return (JObject)JToken.ReadFrom(reader);
        }
    }
}
