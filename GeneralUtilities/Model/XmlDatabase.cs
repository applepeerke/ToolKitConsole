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
            XmlObjects = new List<XmlObject>();
        }
		// Data
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("objects")]
        [XmlArrayItem("object")]
        public List<XmlObject> XmlObjects { get; set; }
    }
}
