using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kentico.Kontent.Management.Modules.ModelBuilders
{
    internal class PropertyMapper : IPropertyMapper
    {
        public bool IsMatch(PropertyInfo modelProperty, string elementName)
        {
            if (elementName == null)
            {
                return false;
            }

            var ignoreAttribute = modelProperty.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ignoreAttribute != null) return false;

            var propertyAttribute = modelProperty.GetCustomAttribute<JsonPropertyAttribute>();

            return propertyAttribute == null
                ? elementName.Replace("_", "").Equals(modelProperty.Name, StringComparison.OrdinalIgnoreCase)
                : elementName.Equals(propertyAttribute.PropertyName, StringComparison.Ordinal);
        }

        public IDictionary<string, string> GetNameMapping(Type type)
        {
            var resolver = new DefaultContractResolver();

            if (resolver.ResolveContract(type) is JsonObjectContract contract)
            {
                return contract.Properties.Where(x => !x.Ignored).ToDictionary(x => x.UnderlyingName, x => x.PropertyName.ToLower());
            }

            return new Dictionary<string, string>();
        }
    }
}
