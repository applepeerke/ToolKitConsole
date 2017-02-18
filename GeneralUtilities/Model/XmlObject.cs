using System.Collections.Generic;
using System.Xml.Serialization;

namespace GeneralUtilities
{
    public class XmlObject
    {
        public XmlObject()
        {
        }

        public XmlObject( string name)
        {
            Name = name;
            XmlAttributes = new List<XmlObjectAttribute>();
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("attributes")]
		[XmlArrayItem("attribute")]
        public List<XmlObjectAttribute> XmlAttributes { get; set; } 
    }
}
