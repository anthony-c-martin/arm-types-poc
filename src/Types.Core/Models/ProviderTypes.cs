using System;
using System.Collections.Generic;

namespace Types.Core.Models
{
    public class ProviderTypes
    {
        public ProviderTypes(string provider, string apiVersion)
        {
            Provider = provider;
            ApiVersion = apiVersion;
        }

        public string Provider { get; }

        public string ApiVersion { get; }
    }
}