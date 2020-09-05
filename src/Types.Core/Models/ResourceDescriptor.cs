using System;
using System.Collections.Generic;

namespace Types.Core.Models
{
    public class ResourceDescriptor
    {
        public ResourceDescriptor(ScopeType scopeType, string providerNamespace, IReadOnlyList<string> resourceTypeSegments, string apiVersion, bool hasVariableName)
        {
            ScopeType = scopeType;
            ProviderNamespace = providerNamespace;
            ResourceTypeSegments = resourceTypeSegments;
            ApiVersion = apiVersion;
            HasVariableName = hasVariableName;
        }

        public ScopeType ScopeType { get; }  

        public string ProviderNamespace { get; }

        public IReadOnlyList<string> ResourceTypeSegments { get; }

        public string ApiVersion { get; }

        public bool HasVariableName { get; }

        public string FullyQualifiedType => FormatFullyQualifiedType(ProviderNamespace, ResourceTypeSegments);

        public static string FormatFullyQualifiedType(string providerNamespace, IEnumerable<string> resourceTypeSegments)
            => $"{providerNamespace}/{FormatUnqualifiedType(resourceTypeSegments)}";

        public static string FormatUnqualifiedType(IEnumerable<string> resourceTypeSegments)
            => string.Join("/", resourceTypeSegments);

        public static IEqualityComparer<ResourceDescriptor> Comparer { get; }
            = new EqualityComparer();

        private class EqualityComparer : IEqualityComparer<ResourceDescriptor>
        {
            public bool Equals(ResourceDescriptor x, ResourceDescriptor y)
                => x.ScopeType == y.ScopeType &&
                    StringComparer.OrdinalIgnoreCase.Equals(x.FullyQualifiedType, y.FullyQualifiedType) &&
                    StringComparer.OrdinalIgnoreCase.Equals(x.ApiVersion, y.ApiVersion) &&
                    x.HasVariableName == y.HasVariableName;

            public int GetHashCode(ResourceDescriptor obj)
                => obj.ScopeType.GetHashCode() ^
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FullyQualifiedType) ^
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ApiVersion) ^
                    obj.HasVariableName.GetHashCode();
        }
    }
}