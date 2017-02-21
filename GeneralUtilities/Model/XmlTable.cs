using System.Collections.Generic;
using System.Xml.Serialization;

namespace GeneralUtilities
{
    public class XmlTable
    {
        public XmlTable()
        {
        }

        public XmlTable( string name)
        {
            Name = name;
            XmlColumns = new List<XmlTableColumn>();
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("columns")]
		[XmlArrayItem("column")]
        public List<XmlTableColumn> XmlColumns { get; set; } 
    }
}
