using System.Collections.Generic;
using System.Xml.Serialization;

namespace GeneralUtilities
{
    public class XmlDatabase
    {
		// Constructors
        public XmlDatabase(string name)
        {
            Name = name;
            XmlTables = new List<XmlTable>();
        }
		// Data
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("tables")]
        [XmlArrayItem("table")]
        public List<XmlTable> XmlTables { get; set; }
    }
}
