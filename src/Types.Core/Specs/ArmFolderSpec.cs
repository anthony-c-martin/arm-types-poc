namespace Types.Core.Specs
{
    public class ArmFolderSpec
    {
        public ArmFolderSpec(string fullPath, string name, string provider, string prefix, string apiVersion)
        {
            FullPath = fullPath;
            Name = name;
            Provider = provider;
            Prefix = prefix;
            ApiVersion = apiVersion;
        }

        public string FullPath { get; }

        public string Name { get; }

        public string Provider { get; }

        public string Prefix { get; }

        public string ApiVersion { get; }
    }
}