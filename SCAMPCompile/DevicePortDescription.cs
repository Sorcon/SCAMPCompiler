using System;

[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class Device
{
    [System.Xml.Serialization.XmlElementAttribute("Range", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = true)]
    public ValueRange[] Range = new ValueRange[0];

    [System.Xml.Serialization.XmlElementAttribute("Port", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = true)]
    public DevicePort[] Port = new DevicePort[0];

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name;

    public DevicePort GetPortByAddress(UInt32 address)
    {
        DevicePort res = null;
        foreach (var port in Port)
        {
            if (res == null && port.Address <= address)
            {
                res = port;
            }
            else if (port.Address <= address && res.Address < port.Address)
                res = port;
        }
        foreach (var r in Range)
        {
            if (r.Name == res.Range)
                res.RangeObj = r;
        }
        return res;
    }
}

[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class DevicePort
{
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public int Address;

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public int Width;

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Range;

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Alias;

    [System.Xml.Serialization.XmlTextAttribute()]
    public string Description;

    [System.Xml.Serialization.XmlIgnore]
    public ValueRange RangeObj { get; set; }
}

[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class ValueRange
{
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public int Min;

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public int Max;

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Name;

    //[System.Xml.Serialization.XmlTextAttribute()]
    //public string Description;
}