using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SCAMPCompile.DeviceParser
{

    public class DeviceXmlParser
    {
        Regex _regex = new Regex(@"(?<first>\<[\?]{0,1}(?<name>[\w\-]{0,})(?<prop>[\s]{1,}(?<propName>[\w]{1,})=""(?<propVal>[\w\s\.\-]{1,}){0,}""){0,}([\s\?\/]{0,}[\/\>|\>]{1}))(?<value>[\w\s\,\-\.\$\'\(\\:\|"")\/]{0,}){0,}", RegexOptions.Compiled);
        public List<DeviceXmlNode> Nodes { get; } = new List<DeviceXmlNode>();

        string xml { get; }
        public DeviceXmlParser(string _xml)
        {
            this.xml = _xml;
            Parse(_xml);
        }

        public DeviceXmlNode GetNode(string name) =>
            this.Nodes.FirstOrDefault(n => n.Name.CompareTo(name) == 0);

        public string GetPropertyValue(string nodeName, string prop)
        {
            var node = this.GetNode(nodeName);
            return node.GetPropertyValue(prop);
        }
                
        void Parse(string _xml)
        {
            var strings = _xml.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int s=0; s<strings.Length; s++)
            {
                string row = strings[s];
                MatchCollection matches = _regex.Matches(row);
                if (matches.Count != 0)
                {
                    List<DeviceXmlNodeProperty> props = new List<DeviceXmlNodeProperty>();
                    var n = matches[0];


                    var group = n.Groups["name"];
                    string name = group.Value;
                    string val = n.Groups["value"]?.Value ?? "";

                    for (int p = 0; p <  n.Groups["propName"].Captures.Count; p++)
                    {
                        DeviceXmlNodeProperty _prop = new DeviceXmlNodeProperty();
                        
                        _prop.Name = n.Groups["propName"].Captures[p].Value;
                        _prop.Value = n.Groups["propVal"].Captures[p].Value;
                        props.Add(_prop);
                    }
                    DeviceXmlNode _node = new DeviceXmlNode(name, val, props);

                    Nodes.Add(_node);
                }
            }
        }
    }
}
