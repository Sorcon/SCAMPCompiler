using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCAMPCompile.DeviceParser
{
    public class DeviceXmlNode
    {
        public List<DeviceXmlNodeProperty> Properties { get; }

        public string Name { get; }

        public string Value { get; }

        public DeviceXmlNode(string name, string value, List<DeviceXmlNodeProperty> Properties)
        {
            this.Name = name;
            this.Value = value;
            this.Properties = new List<DeviceXmlNodeProperty>(Properties);
        }

        DeviceXmlNodeProperty GetProperty(string name) 
            => Properties.FirstOrDefault(p => p.Name.CompareTo(name) == 0);

        public string GetPropertyValue(string name)
        {
            return this.GetProperty(name)?.Value;            
        }
        
    }
}
