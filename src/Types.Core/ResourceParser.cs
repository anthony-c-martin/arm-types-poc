using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Types.Core.Models;

namespace Types.Core
{
    public class SchemaParser
    {

    }

    public class ResourceParser
    {
        // todo implement logging
        public static void LogMessage(string message)
            => Console.WriteLine($"[INF]: {message}");

        // todo implement logging
        public static void LogWarning(string message)
            => Console.WriteLine($"[WRN]: {message}");

        // todo implement logging
        public static void LogError(string message)
            => Console.WriteLine($"[ERR]: {message}");

        private static readonly Regex parentScopePrefix = new Regex("^.*/providers/", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        private static readonly Regex managementGroupPrefix = new Regex("^/providers/Microsoft.Management/managementGroups/{\\w+}/$", RegexOptions.IgnoreCase);
        private static readonly Regex tenantPrefix = new Regex("^/$", RegexOptions.IgnoreCase);
        private static readonly Regex subscriptionPrefix = new Regex("^/subscriptions/{\\w+}/$", RegexOptions.IgnoreCase);
        private static readonly Regex resourceGroupPrefix = new Regex("^/subscriptions/{\\w+}/resourceGroups/{\\w+}/$", RegexOptions.IgnoreCase);

        private static bool ShouldProcess(string methodPath, OpenApiPathItem pathItem, string apiVersion)
        {
            if (!pathItem.Operations.ContainsKey(OperationType.Put))
            {
                // only PUT operations supported for templates!
                return false;
            }

            // todo do we need the old checks that used to be here?
            return true;
        }

        public static void Parse(OpenApiDocument document)
        {
            var apiVersion = document.Info.Version;
            var providers = new Dictionary<string, ProviderTypes>(StringComparer.OrdinalIgnoreCase);

            foreach (var method in document.Paths)
            {
                if (!ShouldProcess(method.Key, method.Value, apiVersion))
                {
                    continue;
                }

                var putOperation = method.Value.Operations[OperationType.Put];

                var (success, failureReason, resourceDescriptors) = ParseMethod(method.Key, putOperation, apiVersion);
                if (!success)
                {
                    LogWarning($"Skipping path '{method.Key}': {failureReason}");
                    continue;
                }

                foreach (var descriptor in resourceDescriptors)
                {
                    if (!providers.ContainsKey(descriptor.ProviderNamespace))
                    {
                        providers[descriptor.ProviderNamespace] = new ProviderTypes(descriptor.ProviderNamespace, apiVersion);
                    }
                    var provider = providers[descriptor.ProviderNamespace];
                    
                    /*
                    var baseSchema = new JsonSchema
                    {
                        JsonType = "object",
                        ResourceType = descriptor.FullyQualifiedType,
                        Description = descriptor.FullyQualifiedType,
                    };

                    ResourceName resourceName;
                    (success, failureReason, resourceName) = ParseNameSchema(serviceClient, method, providerDefinition, descriptor);
                    if (!success)
                    {
                        LogWarning($"Skipping resource type {descriptor.FullyQualifiedType} under path '{method.Url}': {failureReason}");
                        continue;
                    }

                    if (method.Body?.ModelType is CompositeType body)
                    {
                        foreach (var property in body.ComposedProperties)
                        {
                            if (property.SerializedName == null)
                            {
                                continue;
                            }

                            if (baseSchema.Properties != null && baseSchema.Properties.Keys.Contains(property.SerializedName))
                            {
                                continue;
                            }

                            var propertyDefinition = ParseType(property, property.ModelType, providerDefinition.SchemaDefinitions, serviceClient.ModelTypes);
                            if (propertyDefinition != null)
                            {
                                baseSchema.AddProperty(property.SerializedName, propertyDefinition, property.IsRequired || property.SerializedName == "properties");
                            }
                        }

                        HandlePolymorphicType(baseSchema, body, providerDefinition.SchemaDefinitions, serviceClient.ModelTypes);
                    }

                    providerDefinition.ResourceDefinitions.Add(new ResourceDefinition
                    {
                        BaseSchema = baseSchema,
                        Descriptor = descriptor,
                        Name = resourceName,
                    });
                    */
                }
            }
/*
            return providers.ToDictionary(
                kvp => kvp.Key, 
                kvp => CreateSchema(kvp.Value), 
                StringComparer.OrdinalIgnoreCase);
*/
        }

        private static (bool success, string failureReason, IEnumerable<ResourceDescriptor> resourceDescriptors) ParseMethod(string methodPath, OpenApiOperation operation, string apiVersion)
        {
            var finalProvidersMatch = parentScopePrefix.Match(methodPath);
            if (!finalProvidersMatch.Success)
            {
                return (false, "Unable to locate '/providers/' segment", Enumerable.Empty<ResourceDescriptor>());
            }

            var parentScope = methodPath.Substring(0, finalProvidersMatch.Length - "providers/".Length);
            var routingScope = methodPath.Substring(finalProvidersMatch.Length);

            var providerNamespace = routingScope.Substring(0, routingScope.IndexOf('/'));
            if (IsPathVariable(providerNamespace))
            {
                return (false, $"Unable to process parameterized provider namespace '{providerNamespace}'", Enumerable.Empty<ResourceDescriptor>());
            }

            var (success, failureReason, resourceTypesFound) = ParseResourceTypes(methodPath, operation, routingScope);
            if (!success)
            {
                return (false, failureReason, Enumerable.Empty<ResourceDescriptor>());
            }

            var resNameParam = routingScope.Substring(routingScope.LastIndexOf('/') + 1);
            var hasVariableName = IsPathVariable(resNameParam);

            var scopeType = ScopeType.Unknown;
            if (tenantPrefix.IsMatch(parentScope))
            {
                scopeType = ScopeType.Tenant;
            }
            else if (managementGroupPrefix.IsMatch(parentScope))
            {
                scopeType = ScopeType.ManagementGroup;
            }
            else if (resourceGroupPrefix.IsMatch(parentScope))
            {
                scopeType = ScopeType.ResourceGroup;
            }
            else if (subscriptionPrefix.IsMatch(parentScope))
            {
                scopeType = ScopeType.Subcription;
            }
            else if (parentScopePrefix.IsMatch(parentScope))
            {
                scopeType = ScopeType.Extension;
            }

            return (true, string.Empty, resourceTypesFound.Select(type => new ResourceDescriptor(
                scopeType,
                providerNamespace,
                type.ToList(),
                apiVersion,
                hasVariableName)));
        }

/*
        private static (bool success, string failureReason, ResourceName name) ParseNameSchema(string methodPath, OpenApiPathItem pathItem, ProviderTypes provider, ResourceDescriptor descriptor)
        {
            var finalProvidersMatch = parentScopePrefix.Match(methodPath);
            var routingScope = methodPath.Substring(finalProvidersMatch.Length);

            // get the resource name parameter, e.g. {fooName}
            var resNameParam = routingScope.Substring(routingScope.LastIndexOf('/') + 1);

            if (IsPathVariable(resNameParam))
            {
                // strip the enclosing braces
                resNameParam = TrimParamBraces(resNameParam);

                // look up the type
                var param = pathItem.Parameters.FirstOrDefault(p => p.Name == resNameParam);

                if (param == null)
                {
                    return (false, $"Unable to locate parameter with name '{resNameParam}'", null);
                }

                var nameSchema = ParseType(param.ClientProperty, param.ModelType, providerDefinition.SchemaDefinitions, codeModel.ModelTypes);
                nameSchema.ResourceType = resNameParam;

                if (nameSchema?.Enum?.Count == 1)
                {
                    // Resource name is a constant
                    return (true, string.Empty, CreateConstantResourceName(descriptor, nameSchema.Enum.Single(), nameSchema.Description));
                }

                return (true, string.Empty, new ResourceName
                {
                    HasConstantName = false,
                    NameString = string.Empty,
                    NameSchema = nameSchema,
                });
            }

            if (!resNameParam.All(c => char.IsLetterOrDigit(c)))
            {
                return (false, $"Unable to process non-alphanumeric name '{resNameParam}'", null);
            }

            // Resource name is a constant
            return (true, string.Empty, CreateConstantResourceName(descriptor, resNameParam));
        }*/

        private static (bool success, string failureReason, IEnumerable<IEnumerable<string>> resourceTypesFound) ParseResourceTypes(string methodPath, OpenApiOperation operation, string routingScope)
        {
            var nameSegments = routingScope.Split('/').Skip(1).Where((_, i) => i % 2 == 0);

            if (nameSegments.Count() == 0)
            {
                return (false, $"Unable to find name segments", Enumerable.Empty<IEnumerable<string>>());
            }

            IEnumerable<IEnumerable<string>> resourceTypes = new[] { Enumerable.Empty<string>() };
            foreach (var nameSegment in nameSegments)
            {
                if (IsPathVariable(nameSegment))
                {
                    var parameterName = TrimParamBraces(nameSegment);
                    var parameter = operation.Parameters.FirstOrDefault(methodParameter => methodParameter.Name == parameterName);
                    if (parameter == null)
                    {
                        return (false, $"Found undefined parameter reference {nameSegment}", Enumerable.Empty<IEnumerable<string>>());
                    }

                    if (parameter.Schema == null || parameter.Schema.Enum == null)
                    {
                        return (false, $"Parameter reference {nameSegment} is not defined as an enum", Enumerable.Empty<IEnumerable<string>>());
                    }

                    if (!parameter.Schema.Enum.Any())
                    {
                        return (false, $"Parameter reference {nameSegment} is defined as an enum, but doesn't have any specified values", Enumerable.Empty<IEnumerable<string>>());
                    }

                    if (!parameter.Schema.Enum.All(x => x is OpenApiString))
                    {
                        return (false, $"Parameter reference {nameSegment} is defined as an enum, but doesn't have any specified values", Enumerable.Empty<IEnumerable<string>>());
                    }

                    var enumValues = parameter.Schema.Enum.OfType<OpenApiString>();
                    if (enumValues.Count() != parameter.Schema.Enum.Count)
                    {
                        return (false, $"Parameter reference {nameSegment} is defined as an enum, with non-string values", Enumerable.Empty<IEnumerable<string>>());
                    }

                    resourceTypes = resourceTypes.SelectMany(type => enumValues.Select(v => type.Append(v.Value)));
                }
                else
                {
                    resourceTypes = resourceTypes.Select(type => type.Append(nameSegment));
                }
            }

            return (true, string.Empty, resourceTypes);
        }

        private static bool IsPathVariable(string pathSegment)
            => pathSegment.Length > 0 && pathSegment[0] == '{' && pathSegment[pathSegment.Length - 1] == '}';

        private static string TrimParamBraces(string pathSegment)
            => pathSegment.Substring(1, pathSegment.Length - 2);
    }
}
